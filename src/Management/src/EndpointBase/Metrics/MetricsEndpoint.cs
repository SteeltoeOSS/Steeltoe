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

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>
    {
        private readonly SteeltoeExporter _exporter;
        private readonly ILogger<MetricsEndpoint> _logger;

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
                    return GetMetric(request, measurements[request.MetricName], availTags[request.MetricName]);
                }
            }

            return null;
        }

        protected internal MetricsResponse GetMetric(MetricsRequest request, List<MetricSample> measurements, List<MetricTag> availTags)
        {
            return new MetricsResponse(request.MetricName, measurements, availTags);
        }

        private void GetMetricsCollection(out MetricDictionary<List<MetricSample>> measurements, out MetricDictionary<List<MetricTag>> availTags)
        {
            measurements = new MetricDictionary<List<MetricSample>>();
            availTags = new MetricDictionary<List<MetricTag>>();

            var doubleMetrics = _exporter.GetAndClearDoubleMetrics();
            for (int i = 0; i < doubleMetrics.Count; i++)
            {
                var metric = doubleMetrics[i];
                var labels = metric.Labels;
                switch (metric.AggregationType)
                {
                    case AggregationType.DoubleSum:
                        {
                            var doubleSum = metric.Data as SumData<double>;
                            var doubleValue = doubleSum.Sum;

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleValue));

                            AddLabelsToTags(availTags, metric.MetricName, labels);

                            break;
                        }

                    case AggregationType.Summary:
                        {
                            var doubleSummary = metric.Data as SummaryData<double>;

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleSummary.Count));
                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTAL, doubleSummary.Sum));

                            AddLabelsToTags(availTags, metric.MetricName, labels);

                            break;
                        }
                }
            }

            foreach (var metric in _exporter.GetAndClearLongMetrics())
            {
                var labels = metric.Labels;
                switch (metric.AggregationType)
                {
                    case AggregationType.DoubleSum:
                        {
                            var longSum = metric.Data as SumData<long>;
                            var doubleValue = longSum.Sum;

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleValue));
                            AddLabelsToTags(availTags, metric.MetricName, labels);

                            break;
                        }

                    case AggregationType.Summary:
                        {
                            var doubleSummary = metric.Data as SummaryData<long>;

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleSummary.Count));
                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTAL, doubleSummary.Sum));
                            AddLabelsToTags(availTags, metric.MetricName, labels);

                            break;
                        }
                }
            }
        }

        private void AddLabelsToTags(MetricDictionary<List<MetricTag>> availTags, string name, IEnumerable<KeyValuePair<string, string>> labels)
        {
            foreach (var label in labels)
            {
                availTags[name].Add(new MetricTag(label.Key, new HashSet<string>(new List<string> { label.Value })));
            }
        }

        private class MetricDictionary<T>
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