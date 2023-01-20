// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.




using System.Diagnostics.Metrics;
using System.Globalization;

namespace Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

/// <summary>
/// Exporter of OpenTelemetry metrics to Steeltoe Format.
/// </summary>
public class SteeltoeExporter// : MetricsExporter
{
   // internal PullMetricsCollectionManager CollectionManager { get; }

    internal  int ScrapeResponseCacheDurationMilliseconds { get; }

    //   internal override Func<Batch<Metric>, ExportResult> OnExport { get; set; }
    internal MetricsCollection<List<MetricSample>> metricSamples = new();
    internal MetricsCollection<List<MetricTag>>  availTags = new ();

    public  Action Collect { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SteeltoeExporter" /> class.
    /// </summary>
    /// <param name="options">
    /// Options for the exporter.
    /// </param>
    internal SteeltoeExporter(IPullMetricsExporterOptions options)
    {
        ScrapeResponseCacheDurationMilliseconds = options?.ScrapeResponseCacheDurationMilliseconds ?? 5000;
      //  CollectionManager = new PullMetricsCollectionManager(this);
    }

    //public override ExportResult Export(in Batch<Metric> metrics)
    //{
    //    return OnExport(metrics);
    //}

    //internal override ICollectionResponse GetCollectionResponse(Batch<Metric> metrics = default)
    //{
    //    var metricSamples = new MetricsCollection<List<MetricSample>>();
    //    var availTags = new MetricsCollection<List<MetricTag>>();

    //    if (metrics.Count > 0)
    //    {
    //        GetMetricsCollection(metrics, out metricSamples, out availTags);
    //    }

    //    return new SteeltoeCollectionResponse(metricSamples, availTags, DateTime.Now);
    //}

    //internal override ICollectionResponse GetCollectionResponse(ICollectionResponse collectionResponse, DateTime updatedTime)
    //{
    //    var response = (SteeltoeCollectionResponse)collectionResponse;
    //    return new SteeltoeCollectionResponse(response.MetricSamples, response.AvailableTags, DateTime.Now);
    //}

    internal void AddMetrics(System.Diagnostics.Metrics.Instrument instrument, LabeledAggregationStatistics stats)
    {
        if (stats.AggregationStatistics is RateStatistics rateStats)
        {
            //Log.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
            //    rateStats.Delta.HasValue ? rateStats.Delta.Value.ToString(CultureInfo.InvariantCulture) : "");
            if (rateStats.Delta.HasValue)
            {
                var sample = new MetricSample(MetricStatistic.Rate, rateStats.Delta.Value, stats.Labels);
                metricSamples[instrument.Name].Add(sample);
            }
        }
        else if (stats.AggregationStatistics is LastValueStatistics lastValueStats)
        {
            //Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
            //    lastValueStats.LastValue.HasValue ? lastValueStats.LastValue.Value.ToString(CultureInfo.InvariantCulture) : "");
            if (lastValueStats.LastValue.HasValue)
            {
                var sample = new MetricSample(MetricStatistic.Value, lastValueStats.LastValue.Value, stats.Labels);
                metricSamples[instrument.Name].Add(sample);
            }
        }
        else if (stats.AggregationStatistics is HistogramStatistics histogramStats)
        {
            double sum = histogramStats.HistogramSum;
            //  Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStats.Quantiles));
            var sample = new MetricSample(MetricStatistic.Value, sum, stats.Labels);
            metricSamples[instrument.Name].Add(sample);
        }

        //var tags = new List<KeyValuePair<string, string>>();

        //    foreach (KeyValuePair<string, object> tag in metricPoint.Tags)
        //    {
        //        tags.Add(new KeyValuePair<string, string>(tag.Key, tag.Value.ToString()));
        //    }

        //    UpdateAvailableTags(availTags, metric.Name, tags);

        //    if (metric.MetricType.IsHistogram())
        //    {
        //        double sum = metricPoint.GetHistogramSum();

        //        if (metric.Unit == "s")
        //        {
        //            metricSamples[metric.Name].Add(new MetricSample(MetricStatistic.TotalTime, sum, tags));
        //            metricSamples[metric.Name].Add(new MetricSample(MetricStatistic.Max, sum, tags));
        //        }
        //        else
        //        {
        //            metricSamples[metric.Name].Add(new MetricSample(MetricStatistic.Total, sum, tags));
        //        }
        //    }
        //    else if (((int)metric.MetricType & 0b_0000_1111) == 0x0a /* I8 */)
        //    {
        //        metricSamples[metric.Name].Add(metric.MetricType.IsSum()
        //            ? new MetricSample(MetricStatistic.Total, metricPoint.GetSumLong(), tags)
        //            : new MetricSample(MetricStatistic.Value, metricPoint.GetGaugeLastValueLong(), tags));
        //    }
        //    else
        //    {
        //        metricSamples[metric.Name].Add(metric.MetricType.IsSum()
        //            ? new MetricSample(MetricStatistic.Total, metricPoint.GetSumDouble(), tags)
        //            : new MetricSample(MetricStatistic.Value, metricPoint.GetGaugeLastValueDouble(), tags));
        //    }
    }

    private void UpdateAvailableTags(MetricsCollection<List<MetricTag>> availTags, string name, IEnumerable<KeyValuePair<string, string>> labels)
    {
        foreach (KeyValuePair<string, string> label in labels)
        {
            List<MetricTag> currentTags = availTags[name];
            MetricTag existingTag = currentTags.FirstOrDefault(tag => tag.Tag.Equals(label.Key, StringComparison.OrdinalIgnoreCase));

            if (existingTag != null)
            {
                existingTag.Values.Add(label.Value);
            }
            else
            {
                currentTags.Add(new MetricTag(label.Key, new HashSet<string>(new List<string>
                {
                    label.Value
                })));
            }
        }
    }
}
