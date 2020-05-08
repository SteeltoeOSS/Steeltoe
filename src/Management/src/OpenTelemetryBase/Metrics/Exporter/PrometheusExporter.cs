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
                var doubleList = metrics
                .Select(x => (x as ProcessedMetric<double>))
                .ToList();

                DoubleMetrics.AddRange(doubleList);
            }
            else
            {
                var longList = metrics
                .Select(x => (x as ProcessedMetric<long>))
                .ToList();

                LongMetrics.AddRange(longList);
            }

            return Task.FromResult(ExportResult.Success);
        }

        internal List<ProcessedMetric<long>> GetAndClearLongMetrics()
        {
            // TODO harden this so as to not lose data if Export fails.
            List<ProcessedMetric<long>> current = LongMetrics;
            LongMetrics = new List<ProcessedMetric<long>>();
            return current;
        }

        internal List<ProcessedMetric<double>> GetAndClearDoubleMetrics()
        {
            // TODO harden this so as to not lose data if Export fails.
            List<ProcessedMetric<double>> current = DoubleMetrics;
            DoubleMetrics = new List<ProcessedMetric<double>>();
            return current;
        }
    }
}
