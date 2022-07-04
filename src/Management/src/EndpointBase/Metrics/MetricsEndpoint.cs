// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>, IMetricsEndpoint
{
    private readonly SteeltoeExporter _exporter;
    private readonly ILogger<MetricsEndpoint> _logger;

    public MetricsEndpoint(IMetricsEndpointOptions options, IEnumerable<MetricsExporter> exporters, ILogger<MetricsEndpoint> logger = null)
        : base(options)
    {
        _exporter = exporters?.OfType<SteeltoeExporter>().SingleOrDefault() ?? throw new ArgumentNullException(nameof(exporters));
        _logger = logger;
    }

    public new IMetricsEndpointOptions Options => innerOptions as IMetricsEndpointOptions;

    public override IMetricsResponse Invoke(MetricsRequest request)
    {
        GetMetricsCollection(out var measurements, out var availTags);

        var metricNames = new HashSet<string>(measurements.Keys);
        if (request == null)
        {
            return new MetricsListNamesResponse(metricNames);
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

    protected internal List<MetricSample> GetMetricSamplesByTags(MetricsCollection<List<MetricSample>> measurements, string metricName, IEnumerable<KeyValuePair<string, string>> tags)
    {
        IEnumerable<MetricSample> filtered = measurements[metricName];
        var sampleList = new List<MetricSample>();
        if (tags != null && tags.Any())
        {
            filtered = filtered.Where(sample => tags.All(rt => sample.Tags.Any(sampleTag => rt.Key == sampleTag.Key && rt.Value == sampleTag.Value)));
        }

        static MetricSample SumAggregator(MetricSample current, MetricSample next) => new (current.Statistic, current.Value + next.Value, current.Tags);
        static MetricSample MaxAggregator(MetricSample current, MetricSample next) => new (current.Statistic, current.Value > next.Value ? current.Value : next.Value, current.Tags);

        var valueSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Value);
        if (valueSamples.Any())
        {
            var sample = valueSamples.Aggregate(SumAggregator);
            sampleList.Add(new MetricSample(MetricStatistic.Value, sample.Value / valueSamples.Count(), sample.Tags));
        }

        var totalSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Total);
        if (totalSamples.Any())
        {
            sampleList.Add(totalSamples.Aggregate(SumAggregator));
        }

        var totalTimeSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TotalTime);
        if (totalTimeSamples.Any())
        {
            sampleList.Add(totalTimeSamples.Aggregate(SumAggregator));
        }

        var countSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Count);
        if (countSamples.Any())
        {
            sampleList.Add(countSamples.Aggregate(SumAggregator));
        }

        var maxSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Max);
        if (maxSamples.Any())
        {
            var sample = maxSamples.Aggregate(MaxAggregator);
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
        var response = _exporter.CollectionManager.EnterCollect().Result;

        if (response is SteeltoeCollectionResponse collectionResponse)
        {
            metricSamples = collectionResponse.MetricSamples;
            availTags = collectionResponse.AvailableTags;
            return;
        }
        else
        {
            _logger?.LogWarning("Please ensure OpenTelemetry is configured via Steeltoe extension methods.");
        }

        metricSamples = new MetricsCollection<List<MetricSample>>();
        availTags = new MetricsCollection<List<MetricTag>>();

        // TODO: update the response header with actual update time
    }
}
