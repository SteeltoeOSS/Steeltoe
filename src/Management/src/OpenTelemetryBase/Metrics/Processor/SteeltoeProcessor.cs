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

using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Aggregators;
using OpenTelemetry.Metrics.Export;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Processor
{
    public class SteeltoeProcessor : MetricProcessor
    {
        internal List<Metric<long>> LongMetrics;
        internal List<Metric<double>> DoubleMetrics;

        private readonly MetricExporter exporter;
        private readonly Task worker;
        private readonly TimeSpan aggregationInterval;
        private CancellationTokenSource cts;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteeltoeProcessor"/> class.
        /// </summary>
        /// <param name="exporter">Metric exporter instance.</param>
        /// <param name="aggregationInterval">Interval at which metrics are pushed to Exporter.</param>
        public SteeltoeProcessor(MetricExporter exporter, TimeSpan aggregationInterval)
        {
            this.exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));

            // TODO make this thread safe.
            this.LongMetrics = new List<Metric<long>>();
            this.DoubleMetrics = new List<Metric<double>>();
            this.aggregationInterval = aggregationInterval;
            this.cts = new CancellationTokenSource();

            if (exporter == null || aggregationInterval < TimeSpan.MaxValue)
            {
                this.worker = Task.Factory.StartNew(
                    s => this.Worker((CancellationToken)s), this.cts.Token).ContinueWith((task) => Console.WriteLine("error"), TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SteeltoeProcessor"/> class.
        /// </summary>
        /// <param name="exporter">Metric exporter instance.</param>
        public SteeltoeProcessor(MetricExporter exporter)
            : this(exporter, TimeSpan.MaxValue)
        {
        }

        public override void Process(string meterName, string metricName, LabelSet labelSet, Aggregator<long> aggregator)
        {
            var metric = new ProcessedMetric<long>(meterName, metricName, meterName + metricName, labelSet.Labels, aggregator.GetAggregationType());
            metric.Data = aggregator.ToMetricData();
            this.LongMetrics.Add(metric);
        }

        public override void Process(string meterName, string metricName, LabelSet labelSet, Aggregator<double> aggregator)
        {
            var metric = new ProcessedMetric<double>(meterName, metricName, meterName + metricName, labelSet.Labels, aggregator.GetAggregationType());
            metric.Data = aggregator.ToMetricData();
            this.DoubleMetrics.Add(metric);
        }

        internal SummaryData<T> GetMetricByName<T>(string name, List<KeyValuePair<string, string>> labels = null)
            where T : struct
        {
            ProcessedMetric<T> processedMetric;
            if (typeof(T) == typeof(long))
            {
                var filtered = LongMetrics.Where(m => m.MetricName == name).Select(m => m as ProcessedMetric<T>);
                filtered = labels == null ? filtered : filtered.Where(m => m.Labels.Any(label => labels.Contains(label))).ToList();
                processedMetric = filtered.Aggregate(
                    (a, b) =>
                    {
                        var c = new ProcessedMetric<T>(a.MetricNamespace, name, a.MetricDescription, a.Labels, a.AggregationType);
                        switch (a.AggregationType)
                        {
                            case AggregationType.Summary:
                                var aData = a.Data as SummaryData<T>;
                                var bData = b.Data as SummaryData<T>;
                                var aSum = aData.Sum;
                                var bSum = bData.Sum;
                                var aLongSum = Unsafe.As<T, long>(ref aSum);
                                var bLongSum = Unsafe.As<T, long>(ref bSum);
                                var cLongSum = aLongSum + bLongSum;
                                c.Data = new SummaryData<T>()
                                {
                                    Count = aData.Count + bData.Count,
                                    Max = Comparer<T>.Default.Compare(aData.Max, bData.Max) > 0 ? aData.Max : bData.Max,
                                    Min = Comparer<T>.Default.Compare(aData.Max, bData.Max) < 0 ? aData.Min : bData.Min,
                                    Sum = Unsafe.As<long, T>(ref cLongSum)
                                };
                                break;
                            default:
                                break;
                        }

                        return c;
                    });
            }
            else
            {
                var filtered = labels == null ? DoubleMetrics : DoubleMetrics.FindAll(m => m.Labels.Any(label => labels.Contains(label))).ToList();
                processedMetric = filtered.FirstOrDefault(m => m.MetricName == name) as ProcessedMetric<T>;
            }

            return processedMetric.Data as SummaryData<T>;
        }

        internal void Clear()
        {
            this.LongMetrics = new List<Metric<long>>();
            this.DoubleMetrics = new List<Metric<double>>();
        }

        internal void ExportMetrics()
        {
            ExportMetrics(CancellationToken.None).Wait();
        }

        private async Task ExportMetrics(CancellationToken cancellationToken)
        {
            if (this.LongMetrics.Count > 0)
            {
                var metricToExport = this.LongMetrics;
                this.LongMetrics = new List<Metric<long>>();
                await this.exporter.ExportAsync<long>(metricToExport, cancellationToken);
            }

            if (this.DoubleMetrics.Count > 0)
            {
                var metricToExport = this.DoubleMetrics;
                this.DoubleMetrics = new List<Metric<double>>();
                await this.exporter.ExportAsync<double>(metricToExport, cancellationToken);
            }
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(this.aggregationInterval, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();

                    await ExportMetrics(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var remainingWait = this.aggregationInterval - sw.Elapsed;
                    if (remainingWait > TimeSpan.Zero)
                    {
                        await Task.Delay(remainingWait, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                var s = ex.Message;
            }
        }
    }
}
