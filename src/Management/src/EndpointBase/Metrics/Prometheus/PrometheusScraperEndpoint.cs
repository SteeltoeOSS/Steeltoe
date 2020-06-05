// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private string cachedMetrics;

        public PrometheusScraperEndpoint(IPrometheusEndpointOptions options, PrometheusExporter exporter)
            : base(options)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
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
