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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Exporter
{
    public class SteeltoeExporter : MetricExporter
    {
        public SteeltoeExporter()
        {
            // this.Options = options;
            LongMetrics = new List<ProcessedMetric<long>>();
            DoubleMetrics = new List<ProcessedMetric<double>>();
        }

        private List<ProcessedMetric<long>> LongMetrics { get; set; }

        private List<ProcessedMetric<double>> DoubleMetrics { get; set; }

        /// <inheritdoc/>
        public override Task<ExportResult> ExportAsync<T>(List<Metric<T>> metrics, CancellationToken cancellationToken)
        {
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

        public List<ProcessedMetric<long>> GetAndClearLongMetrics()
        {
            // TODO harden this so as to not lose data if Export fails.
            List<ProcessedMetric<long>> current = LongMetrics;
            LongMetrics = new List<ProcessedMetric<long>>();
            return current;
        }

        public List<ProcessedMetric<double>> GetAndClearDoubleMetrics()
        {
            // TODO harden this so as to not lose data if Export fails.
            List<ProcessedMetric<double>> current = DoubleMetrics;
            DoubleMetrics = new List<ProcessedMetric<double>>();
            return current;
        }
    }
}