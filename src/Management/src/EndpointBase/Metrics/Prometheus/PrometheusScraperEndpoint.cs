// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter.Prometheus;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using System;
using PrometheusExporter = Steeltoe.Management.OpenTelemetry.Metrics.Exporter.PrometheusExporter;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusScraperEndpoint : AbstractEndpoint<string>
    {
        private readonly PrometheusExporter _exporter;
        private readonly ILogger<PrometheusScraperEndpoint> _logger;
        private string cachedMetrics;

        public PrometheusScraperEndpoint(IPrometheusEndpointOptions options, PrometheusExporter exporter, ILogger<PrometheusScraperEndpoint> logger = null)
            : base(options)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _logger = logger;
        }

        public new IEndpointOptions Options
        {
            get
            {
                return options as IEndpointOptions;
            }
        }

        public override string Invoke()
        {
            var metrics = _exporter.GetMetricsCollection();

            if (!string.IsNullOrEmpty(metrics))
            {
                cachedMetrics = metrics;
            }

            return cachedMetrics;
        }
    }
}
