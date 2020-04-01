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

        private void GetMetricsCollection(out Dictionary<string, List<MetricSample>> measurements, out Dictionary<string, List<MetricTag>> availTags)
        {
            measurements = new Dictionary<string, List<MetricSample>>();
            availTags = new Dictionary<string, List<MetricTag>>();

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
                            if (!measurements.ContainsKey(metric.MetricName))
                            {
                                measurements[metric.MetricName] = new List<MetricSample>();
                            }

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleValue));

                            if (!availTags.ContainsKey(metric.MetricName))
                            {
                                availTags[metric.MetricName] = new List<MetricTag>();
                            }

                            foreach (var label in labels)
                            {
                                availTags[metric.MetricName].Add(new MetricTag(label.Key, new HashSet<string>(new List<string> { label.Value })));
                            }

                            break;
                        }

                    case AggregationType.Summary:
                        {
                            var doubleSummary = metric.Data as SummaryData<double>;
                            if (!measurements.ContainsKey(metric.MetricName))
                            {
                                measurements[metric.MetricName] = new List<MetricSample>();
                            }

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleSummary.Count));
                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTAL, doubleSummary.Sum));

                            if (!availTags.ContainsKey(metric.MetricName))
                            {
                                availTags[metric.MetricName] = new List<MetricTag>();
                            }

                            foreach (var label in labels)
                            {
                                availTags[metric.MetricName].Add(new MetricTag(label.Key, new HashSet<string>(new List<string> { label.Value })));
                            }

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
                            var doubleSum = metric.Data as SumData<long>;
                            var doubleValue = doubleSum.Sum;
                            if (measurements[metric.MetricName] == null)
                            {
                                measurements[metric.MetricName] = new List<MetricSample>();
                            }

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleValue));

                            if (availTags[metric.MetricName] == null)
                            {
                                availTags[metric.MetricName] = new List<MetricTag>();
                            }

                            foreach (var label in labels)
                            {
                                availTags[metric.MetricName].Add(new MetricTag(label.Key, new HashSet<string>(new List<string> { label.Value })));
                            }

                            break;
                        }

                    case AggregationType.Summary:
                        {
                            var doubleSummary = metric.Data as SummaryData<long>;
                            if (measurements[metric.MetricName] == null)
                            {
                                measurements[metric.MetricName] = new List<MetricSample>();
                            }

                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.COUNT, doubleSummary.Count));
                            measurements[metric.MetricName].Add(new MetricSample(MetricStatistic.TOTAL, doubleSummary.Sum));

                            foreach (var label in labels)
                            {
                                availTags[metric.MetricName].Add(new MetricTag(label.Key, new HashSet<string>(new List<string> { label.Value })));
                            }

                            break;
                        }
                }
            }
        }
    }
}
