// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using OpenCensus.Trace;
using OpenCensus.Trace.Export;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    public class TraceExporter : ITraceExporter
    {
        private const string EXPORTER_NAME = "ZipkinTraceExporter";
        private ITraceExporterOptions _options;
        private ILogger<TraceExporter> _logger;
        private IExportComponent _exportComponent;
        private TraceExporterHandler _handler;
        private object _lck = new object();

        public TraceExporter(ITraceExporterOptions options, ITracing tracing, ILogger<TraceExporter> logger = null)
        {
            _options = options;
            _logger = logger;
            _exportComponent = tracing.ExportComponent;
        }

        public void Start()
        {
            lock (_lck)
            {
                if (_handler != null)
                {
                    return;
                }

                _handler = new TraceExporterHandler(_options, _logger);
                _exportComponent.SpanExporter.RegisterHandler(EXPORTER_NAME, _handler);
            }
        }

        public void Stop()
        {
            lock (_lck)
            {
                if (_handler == null)
                {
                    return;
                }

                _exportComponent.SpanExporter.UnregisterHandler(EXPORTER_NAME);
                _handler = null;
            }
        }
    }
}