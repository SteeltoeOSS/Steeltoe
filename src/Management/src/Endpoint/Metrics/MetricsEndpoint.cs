// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Exporters;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>, IMetricsEndpoint
{
    private readonly SteeltoeExporter _exporter;
    private readonly ILogger<MetricsEndpoint> _logger;

    public new IMetricsEndpointOptions Options => options as IMetricsEndpointOptions;

    public MetricsEndpoint(IMetricsEndpointOptions options, SteeltoeExporter exporter, ILogger<MetricsEndpoint> logger = null)
        : base(options)
    {
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter), $"Exporters must contain a single {nameof(SteeltoeExporter)}.");
        _logger = logger;
    }

    public override IMetricsResponse Invoke(MetricsRequest request)
    {
        (MetricsCollection<List<MetricSample>> measurements, MetricsCollection<List<MetricTag>> availTags) = GetMetrics();

        var metricNames = new HashSet<string>(measurements.Keys);

        if (request == null)
        {
            return new MetricsListNamesResponse(metricNames);
        }

        if (metricNames.Contains(request.MetricName))
        {
            _logger?.LogTrace("Fetching metrics for " + request.MetricName);
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
        try
        {
            IEnumerable<MetricSample> rateSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Rate).ToList();

            if (rateSamples.Any())
            {
                MetricSample sample = rateSamples.Aggregate(SumAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.Rate, sample.Value / rateSamples.Count(), sample.Tags));
            }

            IEnumerable<MetricSample> valueSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Value).ToList();

            if (valueSamples.Any())
            {
                MetricSample sample = valueSamples.Aggregate(SumAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.Value, sample.Value / valueSamples.Count(), sample.Tags));
            }

            IEnumerable<MetricSample> totalSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Total).ToList();

            if (totalSamples.Any())
            {
                sampleList.Add(totalSamples.Aggregate(SumAggregator));
            }

            IEnumerable<MetricSample> totalTimeSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TotalTime).ToList();

            if (totalTimeSamples.Any())
            {
                sampleList.Add(totalTimeSamples.Aggregate(SumAggregator));
            }

            IEnumerable<MetricSample> countSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Count).ToList();

            if (countSamples.Any())
            {
                sampleList.Add(countSamples.Aggregate(SumAggregator));
            }

            IEnumerable<MetricSample> maxSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Max).ToList();

            if (maxSamples.Any())
            {
                MetricSample sample = maxSamples.Aggregate(MaxAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.Max, sample.Value, sample.Tags));
            }
        }
        catch (Exception)
        {
            // Nothing we can do , log and move on 
        }
        return sampleList;
    }

    protected internal MetricsResponse GetMetric(MetricsRequest request, List<MetricSample> metricSamples, List<MetricTag> availTags)
    {
        return new MetricsResponse(request.MetricName, metricSamples, availTags);
    }

    protected internal (MetricsCollection<List<MetricSample>> Samples, MetricsCollection<List<MetricTag>> Tags) GetMetrics() => _exporter.Export();
}
