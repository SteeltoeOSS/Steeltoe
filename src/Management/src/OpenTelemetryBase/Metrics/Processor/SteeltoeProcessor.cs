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
        internal Dictionary<MetricKey, Metric<long>> LongMetrics;
        internal Dictionary<MetricKey, Metric<double>> DoubleMetrics;

        private readonly MetricExporter exporter;
        private readonly Task worker;
        private readonly TimeSpan exportInterval;
        private readonly CancellationTokenSource cts;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteeltoeProcessor"/> class.
        /// </summary>
        /// <param name="exporter">Metric exporter instance.</param>
        /// <param name="exportInterval">Interval at which metrics are pushed to Exporter.</param>
        public SteeltoeProcessor(MetricExporter exporter, TimeSpan exportInterval)
        {
            this.exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));

            // TODO make this thread safe.
            this.LongMetrics = new Dictionary<MetricKey, Metric<long>>();
            this.DoubleMetrics = new Dictionary<MetricKey, Metric<double>>();
            this.exportInterval = exportInterval;
            this.cts = new CancellationTokenSource();

            if (exporter == null || exportInterval < TimeSpan.MaxValue)
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
            var metricKey = new MetricKey(metricName, labelSet.Labels);
            if (this.LongMetrics.ContainsKey(metricKey))
            {
                var previousMetric = LongMetrics[metricKey];
                switch (previousMetric.AggregationType)
                {
                    case AggregationType.LongSum:
                        LongMetrics[metricKey] = UpdateSum(metric, previousMetric);
                        break;
                    case AggregationType.Summary:
                        LongMetrics[metricKey] = UpdateSummary(metric, previousMetric);
                        break;
                }
            }
            else
            {
                LongMetrics.Add(metricKey, metric);
            }
        }

        public override void Process(string meterName, string metricName, LabelSet labelSet, Aggregator<double> aggregator)
        {
            var metric = new ProcessedMetric<double>(meterName, metricName, meterName + metricName, labelSet.Labels, aggregator.GetAggregationType());
            metric.Data = aggregator.ToMetricData();
            var metricKey = new MetricKey(metricName, labelSet.Labels);

            if (this.DoubleMetrics.ContainsKey(metricKey))
            {
                var previousMetric = DoubleMetrics[metricKey];
                switch (previousMetric.AggregationType)
                {
                    case AggregationType.LongSum:
                        DoubleMetrics[metricKey] = UpdateSum(metric, previousMetric);
                        break;
                    case AggregationType.Summary:
                        DoubleMetrics[metricKey] = UpdateSummary(metric, previousMetric);
                        break;
                }
            }
            else
            {
                DoubleMetrics.Add(metricKey, metric);
            }
        }

        internal SummaryData<T> GetMetricByName<T>(string name, List<KeyValuePair<string, string>> labels = null)
            where T : struct
        {
            ProcessedMetric<T> processedMetric;
            if (typeof(T) == typeof(long))
            {
                var filtered = LongMetrics.Values.Where(m => m.MetricName == name).Select(m => m as ProcessedMetric<T>);
                filtered = labels == null ? filtered : filtered.Where(m => m.Labels.Any(label => labels.Contains(label))).ToList();
                processedMetric = filtered.Count() < 1 ? default : filtered.Aggregate(
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
                                    Max = Max(aData.Max, bData.Max),
                                    Min = Min(aData.Min, bData.Min),
                                    Sum = Unsafe.As<long, T>(ref cLongSum),
                                };
                                break;
                            default:
                                // TODO: If we use more than Measure instruments we need more aggregations
                                break;
                        }

                        return c;
                    });
            }
            else
            {
                var filtered = DoubleMetrics.Values.Where(m => m.MetricName == name).Select(m => m as ProcessedMetric<T>);
                filtered = labels == null ? filtered : filtered.Where(m => m.Labels.Any(label => labels.Contains(label))).ToList();

                processedMetric = filtered.Count() < 1 ? default : filtered.Aggregate(
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
                                var aDoubleSum = Unsafe.As<T, double>(ref aSum);
                                var bDoubleSum = Unsafe.As<T, double>(ref bSum);
                                var cDoubleSum = aDoubleSum + bDoubleSum;
                                c.Data = new SummaryData<T>()
                                {
                                    Count = aData.Count + bData.Count,
                                    Max = Max(aData.Max, bData.Max),
                                    Min = Min(aData.Max, bData.Max),
                                    Sum = Unsafe.As<double, T>(ref cDoubleSum),
                                };
                                break;
                            default:
                                // TODO: If we use more than Measure instruments we need more aggregations
                                break;
                        }

                        return c;
                    });
            }

            return processedMetric?.Data as SummaryData<T>;
        }

        internal void Clear()
        {
            this.LongMetrics = new Dictionary<MetricKey, Metric<long>>();
            this.DoubleMetrics = new Dictionary<MetricKey, Metric<double>>();
        }

        internal void ExportMetrics()
        {
            ExportMetrics(CancellationToken.None).Wait();
        }

        private Metric<T> UpdateSum<T>(ProcessedMetric<T> metric, Metric<T> previousMetric)
   where T : struct
        {
            var previousSummary = previousMetric.Data as SumData<T>;
            var currentSummary = metric.Data as SumData<T>;

            var newSum = Sum(previousSummary.Sum, currentSummary.Sum);
            var newSummary = new SumData<T>() { Sum = newSum, Timestamp = currentSummary.Timestamp };
            metric.Data = newSummary;
            return metric;
        }

        private Metric<T> UpdateSummary<T>(ProcessedMetric<T> metric, Metric<T> previousMetric)
    where T : struct
        {
            var previousSummary = previousMetric.Data as SummaryData<T>;
            var currentSummary = metric.Data as SummaryData<T>;
            var newMax = Max(previousSummary.Max, currentSummary.Max);
            var newMin = Min(previousSummary.Min, currentSummary.Min);
            var newCount = previousSummary.Count + currentSummary.Count;
            var newSum = Sum(previousSummary.Sum, currentSummary.Sum);
            var newSummary = new SummaryData<T>() { Count = newCount, Min = newMin, Max = newMax, Sum = newSum, Timestamp = currentSummary.Timestamp };
            metric.Data = newSummary;
            previousMetric = metric;
            return previousMetric;
        }

        private T Sum<T>(T val1, T val2)
           where T : struct
        {
            if (typeof(T) == typeof(long))
            {
                return (T)(object)((long)(object)val1 + (long)(object)val2);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)((double)(object)val1 + (double)(object)val2);
            }

            return default;
        }

        private T Max<T>(T val1, T val2)
            where T : struct
        {
            if (typeof(T) == typeof(long))
            {
                return (T)(object)Math.Max((long)(object)val1, (long)(object)val2);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Max((double)(object)val1, (double)(object)val2);
            }

            return default;
        }

        private T Min<T>(T val1, T val2)
          where T : struct
        {
            if (typeof(T) == typeof(long))
            {
                return (T)(object)Math.Min((long)(object)val1, (long)(object)val2);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Min((double)(object)val1, (double)(object)val2);
            }

            return default;
        }

        private async Task ExportMetrics(CancellationToken cancellationToken)
        {
            if (this.LongMetrics.Count > 0)
            {
                var metricToExport = this.LongMetrics.Values.ToList();
                await this.exporter.ExportAsync<long>(metricToExport, cancellationToken);
            }

            if (this.DoubleMetrics.Count > 0)
            {
                var metricToExport = this.DoubleMetrics.Values.ToList();
                await this.exporter.ExportAsync<double>(metricToExport, cancellationToken);
            }
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(this.exportInterval, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();

                    await ExportMetrics(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var remainingWait = this.exportInterval - sw.Elapsed;
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

        internal struct MetricKey
        {
            public string MetricName;
            public List<KeyValuePair<string, string>> LabelSet;

            public MetricKey(string name, IEnumerable<KeyValuePair<string, string>> labelSet)
            {
                this.MetricName = name;
                this.LabelSet = labelSet.ToList();
            }
        }
    }
}
