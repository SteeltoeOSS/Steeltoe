// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.MetricCollectors.Metrics;
using Steeltoe.Management.MetricCollectors.SystemDiagnosticsMetrics;

namespace Steeltoe.Management.MetricCollectors.Exporters;

/// <summary>
/// Exporter metrics to Steeltoe Format.
/// </summary>
internal sealed class SteeltoeExporter
{
    // ReSharper disable once CollectionNeverUpdated.Local
    private readonly MetricsCollection<IList<MetricSample>> _metricSamples = new();
    private readonly MetricsCollection<IList<MetricTag>> _availableTags = new();

    private readonly int _cacheDurationMilliseconds;
    private readonly object _collectionLock = new();
    private MetricsCollection<IList<MetricSample>> _lastCollectionSamples = new();
    private MetricsCollection<IList<MetricTag>> _lastAvailableTags = new();
    private DateTime _lastCollection = DateTime.MinValue;
    private Action? _collect;

    /// <summary>
    /// Initializes a new instance of the <see cref="SteeltoeExporter" /> class.
    /// </summary>
    /// <param name="options">
    /// Options for the exporter.
    /// </param>
    public SteeltoeExporter(MetricsExporterOptions options)
    {
        ArgumentGuard.NotNull(options);

        _cacheDurationMilliseconds = options.CacheDurationMilliseconds;
    }

    public void SetCollect(Action collect)
    {
        _collect = collect;
    }

    public (MetricsCollection<IList<MetricSample>> MetricSamples, MetricsCollection<IList<MetricTag>> AvailableTags) Export()
    {
        if (_collect == null)
        {
            throw new InvalidOperationException("Collect should not be null");
        }

        lock (_collectionLock)
        {
            if (DateTime.Now > _lastCollection.AddMilliseconds(_cacheDurationMilliseconds))
            {
                _metricSamples.Clear();
                _availableTags.Clear();
                _collect(); // Calls aggregation Manager.Collect
                _lastCollectionSamples = new MetricsCollection<IList<MetricSample>>(_metricSamples);
                _lastAvailableTags = new MetricsCollection<IList<MetricTag>>(_availableTags);
                _lastCollection = DateTime.Now;
            }
        }

        return (_lastCollectionSamples, _lastAvailableTags);
    }

    public void AddMetrics(Instrument instrument, LabeledAggregationStatistics stats)
    {
        ArgumentGuard.NotNull(instrument);
        ArgumentGuard.NotNull(stats);

        UpdateAvailableTags(_availableTags, instrument.Name, stats.Labels);

        if (stats.AggregationStatistics is RateStatistics rateStats)
        {
            if (rateStats.Delta.HasValue)
            {
                var sample = new MetricSample(MetricStatistic.Rate, rateStats.Delta.Value, stats.Labels);
                _metricSamples.GetOrAdd(instrument.Name, new List<MetricSample>()).Add(sample);
            }
        }
        else if (stats.AggregationStatistics is LastValueStatistics lastValueStats)
        {
            if (lastValueStats.LastValue.HasValue)
            {
                var sample = new MetricSample(MetricStatistic.Value, lastValueStats.LastValue.Value, stats.Labels);
                _metricSamples.GetOrAdd(instrument.Name, new List<MetricSample>()).Add(sample);
            }
        }
        else if (stats.AggregationStatistics is HistogramStatistics histogramStats)
        {
            double sum = histogramStats.HistogramSum;

            if (instrument.Unit == "s")
            {
                var timeSample = new MetricSample(MetricStatistic.TotalTime, sum, stats.Labels);
                _metricSamples.GetOrAdd(instrument.Name, new List<MetricSample>()).Add(timeSample);

                var maxSample = new MetricSample(MetricStatistic.Max, histogramStats.HistogramMax, stats.Labels);
                _metricSamples.GetOrAdd(instrument.Name, new List<MetricSample>()).Add(maxSample);
            }
            else
            {
                var sample = new MetricSample(MetricStatistic.Total, sum, stats.Labels);
                _metricSamples.GetOrAdd(instrument.Name, new List<MetricSample>()).Add(sample);
            }
        }
    }

    private static void UpdateAvailableTags(MetricsCollection<IList<MetricTag>> availableTags, string name, IEnumerable<KeyValuePair<string, string>> labels)
    {
        foreach (KeyValuePair<string, string> label in labels)
        {
            IList<MetricTag> currentTags = availableTags.GetOrAdd(name, new List<MetricTag>());
            MetricTag? existingTag = currentTags.FirstOrDefault(tag => tag.Tag.Equals(label.Key, StringComparison.OrdinalIgnoreCase));

            if (existingTag != null)
            {
                _ = existingTag.Values.Add(label.Value);
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
