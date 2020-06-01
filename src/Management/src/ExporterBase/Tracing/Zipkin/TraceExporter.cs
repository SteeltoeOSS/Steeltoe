// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenCensus.Trace.Export;
using Steeltoe.Management.Census.Trace;
using System;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    [Obsolete("Use OpenCensus project packages")]
    public class TraceExporter : ITraceExporter
    {
        private const string EXPORTER_NAME = "ZipkinTraceExporter";
        private readonly ITraceExporterOptions _options;
        private readonly ILogger<TraceExporter> _logger;
        private readonly IExportComponent _exportComponent;
        private readonly object _lck = new object();

        private TraceExporterHandler _handler;

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