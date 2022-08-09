// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>, IMetricsEndpoint
{
    private readonly SteeltoeExporter _exporter;
    private readonly ILogger<MetricsEndpoint> _logger;

    public new IMetricsEndpointOptions Options => options as IMetricsEndpointOptions;

    public MetricsEndpoint(IMetricsEndpointOptions options, IEnumerable<MetricsExporter> exporters, ILogger<MetricsEndpoint> logger = null)
        : base(options)
    {
        _exporter = exporters?.OfType<SteeltoeExporter>().SingleOrDefault() ??
            throw new ArgumentException($"Exporters must contain a single {nameof(SteeltoeExporter)}.", nameof(exporters));

        _logger = logger;
    }

    public override IMetricsResponse Invoke(MetricsRequest request)
    {
        GetMetricsCollection(out MetricsCollection<List<MetricSample>> measurements, out MetricsCollection<List<MetricTag>> availTags);

        var metricNames = new HashSet<string>(measurements.Keys);

        if (request == null)
        {
            return new MetricsListNamesResponse(metricNames);
        }

        if (metricNames.Contains(request.MetricName))
        {
            List<MetricSample> sampleList = GetMetricSamplesByTags(measurements, request.MetricName, request.Tags);

            return GetMetric(request, sampleList, availTags[request.MetricName]);
        }

        return null;
    }

    protected internal List<MetricSample> GetMetricSamplesByTags(MetricsCollection<List<MetricSample>> measurements, string metricName,
        IEnumerable<KeyValuePair<string, string>> tags)
    {
        IEnumerable<MetricSample> filtered = measurements[metricName];
        var sampleList = new List<MetricSample>();

        if (tags != null && tags.Any())
        {
            filtered = filtered.Where(sample => tags.All(rt => sample.Tags.Any(sampleTag => rt.Key == sampleTag.Key && rt.Value == sampleTag.Value)));
        }

        static MetricSample SumAggregator(MetricSample current, MetricSample next)
        {
            return new MetricSample(current.Statistic, current.Value + next.Value, current.Tags);
        }

        static MetricSample MaxAggregator(MetricSample current, MetricSample next)
        {
            return new MetricSample(current.Statistic, current.Value > next.Value ? current.Value : next.Value, current.Tags);
        }

        IEnumerable<MetricSample> valueSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Value);

        if (valueSamples.Any())
        {
            MetricSample sample = valueSamples.Aggregate(SumAggregator);
            sampleList.Add(new MetricSample(MetricStatistic.Value, sample.Value / valueSamples.Count(), sample.Tags));
        }

        IEnumerable<MetricSample> totalSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Total);

        if (totalSamples.Any())
        {
            sampleList.Add(totalSamples.Aggregate(SumAggregator));
        }

        IEnumerable<MetricSample> totalTimeSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TotalTime);

        if (totalTimeSamples.Any())
        {
            sampleList.Add(totalTimeSamples.Aggregate(SumAggregator));
        }

        IEnumerable<MetricSample> countSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Count);

        if (countSamples.Any())
        {
            sampleList.Add(countSamples.Aggregate(SumAggregator));
        }

        IEnumerable<MetricSample> maxSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Max);

        if (maxSamples.Any())
        {
            MetricSample sample = maxSamples.Aggregate(MaxAggregator);
            sampleList.Add(new MetricSample(MetricStatistic.Max, sample.Value, sample.Tags));
        }

        return sampleList;
    }

    protected internal MetricsResponse GetMetric(MetricsRequest request, List<MetricSample> metricSamples, List<MetricTag> availTags)
    {
        return new MetricsResponse(request.MetricName, metricSamples, availTags);
    }

    protected internal void GetMetricsCollection(out MetricsCollection<List<MetricSample>> metricSamples, out MetricsCollection<List<MetricTag>> availTags)
    {
        ICollectionResponse response = _exporter.CollectionManager.EnterCollectAsync().Result;

        if (response is SteeltoeCollectionResponse collectionResponse)
        {
            metricSamples = collectionResponse.MetricSamples;
            availTags = collectionResponse.AvailableTags;
            return;
        }

        _logger?.LogWarning("Please ensure OpenTelemetry is configured via Steeltoe extension methods.");

        metricSamples = new MetricsCollection<List<MetricSample>>();
        availTags = new MetricsCollection<List<MetricTag>>();

        // TODO: update the response header with actual update time
    }
}
