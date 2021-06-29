﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Exporter
{
    public class PrometheusExporter : MetricExporter
    {
        public PrometheusExporter()
        {
            LongMetrics = new List<ProcessedMetric<long>>();
            DoubleMetrics = new List<ProcessedMetric<double>>();
        }

        private List<ProcessedMetric<long>> LongMetrics { get; set; }

        private List<ProcessedMetric<double>> DoubleMetrics { get; set; }

        /// <inheritdoc/>
        public override Task<ExportResult> ExportAsync<T>(List<Metric<T>> metrics, CancellationToken cancellationToken)
        {
            // Prometheus uses a pull model, not a push.
            // Accumulate the exported metrics internally, return success.
            // The pull process will read this internally stored metrics
            // at its own schedule.
            if (typeof(T) == typeof(double))
            {
                DoubleMetrics = metrics
                .Select(x => (x as ProcessedMetric<double>))
                .ToList();
            }
            else
            {
                LongMetrics = metrics
                .Select(x => (x as ProcessedMetric<long>))
                .ToList();
            }

            return Task.FromResult(ExportResult.Success);
        }

        internal List<ProcessedMetric<long>> GetAndClearLongMetrics()
        {
            // TODO harden this so as to not lose data if Export fails.
            var current = LongMetrics;
            LongMetrics = new List<ProcessedMetric<long>>();
            return current;
        }

        internal List<ProcessedMetric<double>> GetAndClearDoubleMetrics()
        {
            // TODO harden this so as to not lose data if Export fails.
            var current = DoubleMetrics;
            DoubleMetrics = new List<ProcessedMetric<double>>();
            return current;
        }
    }
}
