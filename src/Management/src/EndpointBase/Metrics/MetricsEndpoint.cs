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
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>
    {
        private readonly SteeltoeExporter _exporter;
        private readonly ILogger<MetricsEndpoint> _logger;

        private List<ProcessedMetric<long>> LongMetrics { get; set; }

        private List<ProcessedMetric<double>> DoubleMetrics { get; set; }

        public MetricsEndpoint(IMetricsOptions options, SteeltoeExporter exporter, ILogger<MetricsEndpoint> logger = null)
            : base(options)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _logger = logger;
        }

        public new IMetricsOptions Options
        {
            get
            {
                return options as IMetricsOptions;
            }
        }

        public override IMetricsResponse Invoke(MetricsRequest request)
        {
            GetMetricsCollection(out var measurements, out var availTags);

            var metricNames = new HashSet<string>(measurements.Keys);
            if (request == null)
            {
                return new MetricsListNamesResponse(new HashSet<string>(measurements.Keys));
            }
            else
            {
                if (metricNames.Contains(request.MetricName))
                {
                    var sampleList = GetMetricSamplesByTags(measurements, request.MetricName, request.Tags);

                    return GetMetric(request, sampleList, availTags[request.MetricName]);
                }
            }

            return null;
        }

        protected internal List<MetricSample> GetMetricSamplesByTags(MetricDictionary<List<MetricSample>> measurements,  string metricName, IEnumerable<KeyValuePair<string, string>> tags)
        {
            IEnumerable<MetricSample> filtered = measurements[metricName];
            var sampleList = new List<MetricSample>();
            if (tags != null && tags.Any())
            {
                filtered = filtered.Where(sample => tags.All(rt => sample.Tags.Any(sampleTag => rt.Key == sampleTag.Key && rt.Value == sampleTag.Value)));
            }

            static MetricSample SumAggregator(MetricSample current, MetricSample next) => new MetricSample(current.Statistic, current.Value + next.Value, current.Tags);

            var valueSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.VALUE);
            if (valueSamples.Any())
            {
               var sample = valueSamples.Aggregate(SumAggregator);
               sampleList.Add(new MetricSample(MetricStatistic.VALUE, sample.Value / valueSamples.Count(), sample.Tags));
            }

            var totalSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TOTAL);
            if (totalSamples.Any())
            {
                sampleList.Add(totalSamples.Aggregate(SumAggregator));
            }

            var totalTimeSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TOTALTIME);
            if (totalTimeSamples.Any())
            {
                sampleList.Add(totalTimeSamples.Aggregate(SumAggregator));
            }

            var countSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.COUNT);
            if (countSamples.Any())
            {
                sampleList.Add(countSamples.Aggregate(SumAggregator));
            }

            return sampleList;
        }

        protected internal MetricsResponse GetMetric(MetricsRequest request, List<MetricSample> measurements, List<MetricTag> availTags)
        {
            return new MetricsResponse(request.MetricName, measurements, availTags);
        }

        protected internal void GetMetricsCollection(out MetricDictionary<List<MetricSample>> measurements, out MetricDictionary<List<MetricTag>> availTags)
        {
            measurements = new MetricDictionary<List<MetricSample>>();
            availTags = new MetricDictionary<List<MetricTag>>();

            var doubleMetrics = _exporter.GetAndClearDoubleMetrics();
            if (doubleMetrics == null || doubleMetrics.Count <= 0)
            {
                doubleMetrics = DoubleMetrics;
            }
            else
            {
                DoubleMetrics = doubleMetrics;
            }

            if (doubleMetrics != null)
            {
                for (var i = 0; i < doubleMetrics.Count; i++)
                {
                    var metric = doubleMetrics[i];
                    var labels = metric.Labels;

                    switch (metric.AggregationType)
                    {
                        case AggregationType.DoubleSum:
                            {
                                var doubleSum = metric.Data as SumData<double>;

                                var doubleValue = doubleSum.Sum;

                                measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleValue, labels));

                                AddLabelsToTags(availTags, metric.MetricName, labels);

                                break;
                            }

                        case AggregationType.Summary:
                            {
                                var doubleSummary = metric.Data as SummaryData<double>;

                                var value = doubleSummary.Count > 0 ? doubleSummary.Sum / doubleSummary.Count : 0;
                                measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.VALUE, value, labels));

                                // If labels contain time, Total time
                                if (labels.Any(l => l.Key.Equals("TimeUnit", StringComparison.OrdinalIgnoreCase)))
                                {
                                    measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTALTIME, doubleSummary.Sum, labels));
                                }
                                else
                                {
                                    measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTAL, doubleSummary.Sum, labels));
                                }

                                AddLabelsToTags(availTags, metric.MetricName, labels);

                                break;
                            }

                        default:
                            _logger.LogDebug($"Handle Agg Type {metric.AggregationType} in doubleMetrics");
                            break;
                    }
                }
            }

            var longMetrics = _exporter.GetAndClearLongMetrics();
            if (longMetrics == null || longMetrics.Count <= 0)
            {
                longMetrics = LongMetrics;
            }
            else
            {
                LongMetrics = longMetrics;
            }

            if (longMetrics != null)
            {
                foreach (var metric in longMetrics)
                {
                    var labels = metric.Labels;
                    switch (metric.AggregationType)
                    {
                        case AggregationType.LongSum:
                            {
                                var longSum = metric.Data as SumData<long>;
                                var longValue = longSum.Sum;

                                measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, longValue, labels));
                                AddLabelsToTags(availTags, metric.MetricName, labels);

                                break;
                            }

                        case AggregationType.Summary:
                            {
                                var longSummary = metric.Data as SummaryData<long>;

                                var value = longSummary.Count > 0 ? longSummary.Sum / longSummary.Count : 0;
                                measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.VALUE, value, labels));

                                // If labels contain time, Total time
                                if (labels.Any(l => l.Key.Equals("TimeUnit", StringComparison.OrdinalIgnoreCase)))
                                {
                                    measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTALTIME, longSummary.Sum, labels));
                                }
                                else
                                {
                                    measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTAL, longSummary.Sum, labels));
                                }

                                AddLabelsToTags(availTags, metric.MetricName, labels);

                                break;
                            }

                        default:
                            _logger.LogDebug($"Handle Agg Type {metric.AggregationType} in longMetrics");
                            break;
                    }
                }
            }
        }

        private void AddLabelsToTags(MetricDictionary<List<MetricTag>> availTags, string name, IEnumerable<KeyValuePair<string, string>> labels)
        {
            foreach (var label in labels)
            {
                var currentTags = availTags[name];
                var existingTag = currentTags.FirstOrDefault(tag => tag.Tag.Equals(label.Key, StringComparison.OrdinalIgnoreCase));

                if (existingTag != null)
                {
                    existingTag.Values.Add(label.Value);
                }
                else
                {
                    currentTags.Add(new MetricTag(label.Key, new HashSet<string>(new List<string> { label.Value })));
                }
            }
        }

        protected internal class MetricDictionary<T>
            : Dictionary<string, T>
            where T : new()
        {
            public MetricDictionary()
            {
            }

            public new T this[string key]
            {
                get
                {
                    if (!ContainsKey(key))
                    {
                        base[key] = new T();
                    }

                    return base[key];
                }
            }
        }
    }
}