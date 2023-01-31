// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.




using System.Diagnostics.Metrics;

namespace Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

/// <summary>
/// Exporter metrics to Steeltoe Format.
/// </summary>
public class SteeltoeExporter
{

    internal  int ScrapeResponseCacheDurationMilliseconds { get; }
    private MetricsCollection<List<MetricSample>> _metricSamples = new();
    private MetricsCollection<List<MetricTag>>  _availTags = new ();

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
    }

    internal (MetricsCollection<List<MetricSample>>, MetricsCollection<List<MetricTag>>) Export()
    {
        if(Collect == null)
        {
            throw new InvalidOperationException("Collect should not be null");
        }

        Collect();
        return (_metricSamples, _availTags);
    }

    internal void AddMetrics(Instrument instrument, LabeledAggregationStatistics stats)
    {
        UpdateAvailableTags(_availTags, instrument.Name, stats.Labels);

        if (stats.AggregationStatistics is RateStatistics rateStats)
        {
            //Log.CounterRateValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
            //    rateStats.Delta.HasValue ? rateStats.Delta.Value.ToString(CultureInfo.InvariantCulture) : "");
            if (rateStats.Delta.HasValue)
            {
                var sample = new MetricSample(MetricStatistic.Rate, rateStats.Delta.Value, stats.Labels);
                _metricSamples[instrument.Name].Add(sample);
            }
        }
        else if (stats.AggregationStatistics is LastValueStatistics lastValueStats)
        {
            //Log.GaugeValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels),
            //    lastValueStats.LastValue.HasValue ? lastValueStats.LastValue.Value.ToString(CultureInfo.InvariantCulture) : "");
            if (lastValueStats.LastValue.HasValue)
            {
                var sample = new MetricSample(MetricStatistic.Value, lastValueStats.LastValue.Value, stats.Labels);
                _metricSamples[instrument.Name].Add(sample);
            }
        }
        else if (stats.AggregationStatistics is HistogramStatistics histogramStats)
        {
            double sum = histogramStats.HistogramSum;
            //  Log.HistogramValuePublished(sessionId, instrument.Meter.Name, instrument.Meter.Version, instrument.Name, instrument.Unit, FormatTags(stats.Labels), FormatQuantiles(histogramStats.Quantiles));
            if (instrument.Unit == "s")
            {
                _metricSamples[instrument.Name].Add(new MetricSample(MetricStatistic.TotalTime, sum, stats.Labels));
                _metricSamples[instrument.Name].Add(new MetricSample(MetricStatistic.Max, histogramStats.HistograMax, stats.Labels));

            }
            else
            {
                var sample = new MetricSample(MetricStatistic.Total, sum, stats.Labels);
                _metricSamples[instrument.Name].Add(sample);
            }
        }
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
