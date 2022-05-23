// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
    [ExportModes(ExportModes.Pull)]
    public class SteeltoeExporter : IMetricsExporter
    {
        internal PullmetricsCollectionManager CollectionManager { get; }

        internal override int ScrapeResponseCacheDurationMilliseconds { get; }

       // private List<Metric> _metricsView;

        /// <summary>
        /// Initializes a new instance of the <see cref="SteeltoeExporter"/> class.
        /// </summary>
        /// <param name="options">Options for the exporter.</param>
        internal SteeltoeExporter(IPullmetricsExporterOptions options)
        {
            ScrapeResponseCacheDurationMilliseconds = options?.ScrapeResponseCacheDurationMilliseconds ?? 5000;
            CollectionManager = new PullmetricsCollectionManager(this);
        }

        public override Func<int, bool> Collect { get; set; }

        public override ExportResult Export(in Batch<Metric> metrics)
        {
           return OnExport(metrics);
        }

        internal override Func<Batch<Metric>, ExportResult> OnExport
        {
            get; set;
        }

        internal override ICollectionResponse GetCollectionResponse(Batch<Metric> metrics = default)
        {
            var metricSamples = new MetricsCollection<List<MetricSample>>();
            var availTags = new MetricsCollection<List<MetricTag>>();

            if (metrics.Count > 0)
            {
                GetMetricsCollection(metrics, out metricSamples, out availTags);
            }

            return new SteeltoeCollectionResponse(metricSamples, availTags, DateTime.Now);
        }

        internal override ICollectionResponse GetCollectionResponse(ICollectionResponse steeltoeCollectionResponse, DateTime updatedTime)
        {
            var collectionResponse = (SteeltoeCollectionResponse)steeltoeCollectionResponse;
            return new SteeltoeCollectionResponse(collectionResponse.MetricSamples, collectionResponse.AvailableTags, DateTime.Now);
        }

        private void GetMetricsCollection(Batch<Metric> metrics, out MetricsCollection<List<MetricSample>> metricSamples, out MetricsCollection<List<MetricTag>> availTags)
        {
            metricSamples = new MetricsCollection<List<MetricSample>>();
            availTags = new MetricsCollection<List<MetricTag>>();

            foreach (var metric in metrics)
            {
                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    var tags = new List<KeyValuePair<string, string>>();
                    foreach (var tag in metricPoint.Tags)
                    {
                        tags.Add(new KeyValuePair<string, string>(tag.Key, tag.Value.ToString()));
                    }

                    UpdateAvailableTags(availTags, metric.Name, tags);

                    if (metric.MetricType.IsHistogram())
                    {
                        var sum = metricPoint.GetHistogramSum();
                        if (metric.Unit == "s")
                        {
                            metricSamples[metric.Name].Add(new MetricSample(MetricStatistic.TOTAL_TIME, sum, tags));
                            metricSamples[metric.Name].Add(new MetricSample(MetricStatistic.MAX, sum, tags));
                        }
                        else
                        {
                            metricSamples[metric.Name].Add(new MetricSample(MetricStatistic.TOTAL, sum, tags));
                        }
                    }
                    else if (((int)metric.MetricType & 0b_0000_1111) == 0x0a /* I8 */)
                    {
                        metricSamples[metric.Name].Add(metric.MetricType.IsSum()
                            ? new MetricSample(MetricStatistic.TOTAL, metricPoint.GetSumLong(), tags)
                            : new MetricSample(MetricStatistic.VALUE, metricPoint.GetGaugeLastValueLong(), tags));
                    }
                    else
                    {
                        metricSamples[metric.Name].Add(metric.MetricType.IsSum()
                            ? new MetricSample(MetricStatistic.TOTAL, metricPoint.GetSumDouble(), tags)
                            : new MetricSample(MetricStatistic.VALUE, metricPoint.GetGaugeLastValueDouble(), tags));
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
