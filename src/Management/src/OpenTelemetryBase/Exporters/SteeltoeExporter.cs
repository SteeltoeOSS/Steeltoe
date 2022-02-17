// <copyright file="PrometheusExporter.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
    /// <summary>
    /// Exporter of OpenTelemetry metrics to Steeltoe Format.
    /// </summary>
    [AggregationTemporality(AggregationTemporality.Cumulative)]
    [ExportModes(ExportModes.Pull)]
    public class SteeltoeExporter : BaseExporter<Metric>, IPullMetricExporter
    {
        private List<Metric> _metricsView;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteeltoeExporter"/> class.
        /// </summary>
        /// <param name="options">Options for the exporter.</param>
        public SteeltoeExporter(SteeltoeExporterOptions options)
        {
        }

        public Func<int, bool> Collect
        {
            get;
            set;
        }

        public override ExportResult Export(in Batch<Metric> metrics)
        {
            _metricsView = new List<Metric>();
            foreach (var metric in metrics)
            {
                _metricsView.Add(metric);
            }

            return ExportResult.Success;
        }

        // public List<Metric> GetMetrics() => _metricsView;
        // TODO: Make Threadsafe
        public void GetMetricsCollection(out MetricsCollection<List<MetricSample>> measurements, out MetricsCollection<List<MetricTag>> availTags)
        {
            measurements = new MetricsCollection<List<MetricSample>>();
            availTags = new MetricsCollection<List<MetricTag>>();

            this.Collect?.Invoke(-1); // Call collect. Todo: Any thread safety issues to handle?

            if (_metricsView == null || _metricsView.Count < 1)
            {
                return;
            }

            for (var i = 0; i < _metricsView.Count; i++)
            {
                var metric = _metricsView[i];

                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    var tags = new List<KeyValuePair<string, string>>();
                    foreach (var tag in metricPoint.Tags)
                    {
                        tags.Add(new KeyValuePair<string, string>(tag.Key, tag.Value.ToString()));
                    }

                    UpdateAvailableTags(availTags, metric.Name, tags);

                    // TODO: MetricType is same for all MetricPoints
                    // within a given Metric, so this check can avoideds
                    // for each MetricPoint
                    // TODO: Handle all types and verify
                    if (metric.MetricType.IsHistogram())
                    {

                        var sum = metricPoint.GetHistogramSum();
                        //  var count = metricPoint.GetHistogramCount();
                        measurements[metric.Name].Add(new MetricSample(MetricStatistic.TOTAL, sum, tags));

                    }
                    else if (((int)metric.MetricType & 0b_0000_1111) == 0x0a /* I8 */)
                    {
                        if (metric.MetricType.IsSum())
                        {
                            measurements[metric.Name].Add(new MetricSample(MetricStatistic.TOTAL, metricPoint.GetSumLong(), tags));
                        }
                        else
                        {
                            measurements[metric.Name].Add(new MetricSample(MetricStatistic.VALUE, metricPoint.GetGaugeLastValueLong(), tags));
                        }
                    }
                    else
                    {
                        if (metric.MetricType.IsSum())
                        {
                            measurements[metric.Name].Add(new MetricSample(MetricStatistic.TOTAL, metricPoint.GetSumDouble(), tags));
                        }
                        else
                        {
                            measurements[metric.Name].Add(new MetricSample(MetricStatistic.VALUE, metricPoint.GetGaugeLastValueDouble(), tags));
                        }
                    }
                }
            }
        }

        private void UpdateAvailableTags(MetricsCollection<List<MetricTag>> availTags, string name, IEnumerable<KeyValuePair<string, string>> labels)
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
    }
}
