// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics.Export;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Exporter
{
    /// <summary>
    /// An Exporter that publishes to multiple Exporters.
    /// </summary>
    [Obsolete("OpenTelemetry Metrics API is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class MultiExporter : MetricExporter
    {
        private readonly IEnumerable<MetricExporter> _exporters;

        public MultiExporter(IEnumerable<MetricExporter> exporters)
        {
            _exporters = exporters;
        }

        public override async Task<ExportResult> ExportAsync<T>(List<Metric<T>> metrics, CancellationToken cancellationToken)
        {
            foreach (var exporter in _exporters)
            {
                await exporter.ExportAsync(metrics, cancellationToken);
            }

            return ExportResult.Success;
        }
    }
}