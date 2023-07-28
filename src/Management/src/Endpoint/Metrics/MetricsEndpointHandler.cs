// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;
using Steeltoe.Management.MetricCollectors.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics;

internal sealed class MetricsEndpointHandler : IMetricsEndpointHandler
{
    private readonly IOptionsMonitor<MetricsEndpointOptions> _options;
    private readonly ISteeltoeExporter _exporter;
    private readonly ILogger<MetricsEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public MetricsEndpointHandler(IOptionsMonitor<MetricsEndpointOptions> options, ISteeltoeExporter exporter, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(exporter);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _exporter = exporter;
        _logger = loggerFactory.CreateLogger<MetricsEndpointHandler>();
    }

    public Task<MetricsResponse> InvokeAsync(MetricsRequest request, CancellationToken cancellationToken)
    {
        (MetricsCollection<List<MetricSample>> measurements, MetricsCollection<List<MetricTag>> availableTags) = GetMetrics();

        var metricNames = new HashSet<string>(measurements.Keys);
        MetricsResponse response;

        if (request == null)
        {
            response = new MetricsResponse(metricNames);
        }
        else
        {
            if (metricNames.Contains(request.MetricName))
            {
                _logger.LogTrace("Fetching metrics for " + request.MetricName);
                IList<MetricSample> sampleList = GetMetricSamplesByTags(measurements, request.MetricName, request.Tags);

                response = GetMetric(request, sampleList, availableTags[request.MetricName]);
            }
            else
            {
                response = null;
            }
        }

        return Task.FromResult(response);
    }

    internal IList<MetricSample> GetMetricSamplesByTags(MetricsCollection<List<MetricSample>> measurements, string metricName,
        IList<KeyValuePair<string, string>> tags)
    {
        List<MetricSample> filtered = measurements[metricName];
        var sampleList = new List<MetricSample>();

        if (tags != null && tags.Any())
        {
            filtered = filtered.Where(sample =>
                tags.All(rt => sample.Tags != null && sample.Tags.Any(sampleTag => rt.Key == sampleTag.Key && rt.Value == sampleTag.Value))).ToList();
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
        catch (Exception ex)
        {
            // Nothing we can do, log and move on
            _logger.LogError(ex, "Error transforming metrics.");
        }

        return sampleList;
    }

    private static MetricsResponse GetMetric(MetricsRequest request, IList<MetricSample> metricSamples, IList<MetricTag> availableTags)
    {
        return new MetricsResponse(request.MetricName, metricSamples, availableTags);
    }

    internal (MetricsCollection<List<MetricSample>> Samples, MetricsCollection<List<MetricTag>> Tags) GetMetrics()
    {
        return _exporter.Export();
    }
}