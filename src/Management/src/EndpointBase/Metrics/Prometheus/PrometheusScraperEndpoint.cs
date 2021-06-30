// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using System;

namespace Steeltoe.Management.Endpoint.Metrics
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class PrometheusScraperEndpoint : AbstractEndpoint<string>, IPrometheusScraperEndpoint
    {
        private readonly PrometheusExporter _exporter;
        private string _cachedMetrics;

        public PrometheusScraperEndpoint(IPrometheusEndpointOptions options, PrometheusExporter exporter)
            : base(options)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        }

        public override string Invoke()
        {
            var metrics = _exporter.GetMetricsCollection();

            if (!string.IsNullOrEmpty(metrics))
            {
                _cachedMetrics = metrics;
            }

            return _cachedMetrics;
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
