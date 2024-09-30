// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal sealed class MetricsEndpointHandler : IMetricsEndpointHandler
{
    private readonly IOptionsMonitor<MetricsEndpointOptions> _optionsMonitor;
    private readonly MetricsExporter _exporter;
    private readonly ILogger<MetricsEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public MetricsEndpointHandler(IOptionsMonitor<MetricsEndpointOptions> optionsMonitor, MetricsExporter exporter, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _exporter = exporter;
        _logger = loggerFactory.CreateLogger<MetricsEndpointHandler>();
    }

    public Task<MetricsResponse?> InvokeAsync(MetricsRequest? request, CancellationToken cancellationToken)
    {
        (MetricsCollection<IList<MetricSample>> measurements, MetricsCollection<IList<MetricTag>> availableTags) = GetMetrics();

        var metricNames = new HashSet<string>(measurements.Keys);
        MetricsResponse? response;

        if (request == null)
        {
            response = new MetricsResponse(metricNames);
        }
        else
        {
            if (metricNames.Contains(request.MetricName))
            {
                _logger.LogTrace("Fetching metrics for {Name}", request.MetricName);
                IList<MetricSample> sampleList = GetMetricSamplesByTags(measurements, request.MetricName, request.Tags);

                response = GetMetric(request, sampleList, availableTags.GetOrAdd(request.MetricName, new List<MetricTag>()));
            }
            else
            {
                response = null;
            }
        }

        return Task.FromResult(response);
    }

    internal IList<MetricSample> GetMetricSamplesByTags(MetricsCollection<IList<MetricSample>> measurements, string metricName,
        IList<KeyValuePair<string, string>> tags)
    {
        IList<MetricSample> filtered = measurements.GetOrAdd(metricName, new List<MetricSample>());
        List<MetricSample> sampleList = [];

        if (tags.Any())
        {
            filtered = filtered.Where(sample =>
                tags.All(tag => sample.Tags != null && sample.Tags.Any(sampleTag => tag.Key == sampleTag.Key && tag.Value == sampleTag.Value))).ToArray();
        }

        try
        {
            MetricSample[] rateSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Rate).ToArray();

            if (rateSamples.Length > 0)
            {
                MetricSample sample = rateSamples.Aggregate(SumAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.Rate, sample.Value / rateSamples.Length, sample.Tags));
            }

            MetricSample[] valueSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Value).ToArray();

            if (valueSamples.Length > 0)
            {
                MetricSample sample = valueSamples.Aggregate(SumAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.Value, sample.Value / valueSamples.Length, sample.Tags));
            }

            MetricSample[] totalSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Total).ToArray();

            if (totalSamples.Length > 0)
            {
                sampleList.Add(totalSamples.Aggregate(SumAggregator));
            }

            MetricSample[] totalTimeSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TotalTime).ToArray();

            if (totalTimeSamples.Length > 0)
            {
                sampleList.Add(totalTimeSamples.Aggregate(SumAggregator));
            }

            MetricSample[] countSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Count).ToArray();

            if (countSamples.Length > 0)
            {
                sampleList.Add(countSamples.Aggregate(SumAggregator));
            }

            MetricSample[] maxSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.Max).ToArray();

            if (maxSamples.Length > 0)
            {
                MetricSample sample = maxSamples.Aggregate(MaxAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.Max, sample.Value, sample.Tags));
            }
        }
        catch (Exception exception)
        {
            // Nothing we can do, log and move on
            _logger.LogError(exception, "Error transforming metrics.");
        }

        return sampleList;

        static MetricSample SumAggregator(MetricSample current, MetricSample next)
        {
            return new MetricSample(current.Statistic, current.Value + next.Value, current.Tags);
        }

        static MetricSample MaxAggregator(MetricSample current, MetricSample next)
        {
            return new MetricSample(current.Statistic, current.Value > next.Value ? current.Value : next.Value, current.Tags);
        }
    }

    private static MetricsResponse GetMetric(MetricsRequest request, IList<MetricSample> metricSamples, IList<MetricTag> availableTags)
    {
        return new MetricsResponse(request.MetricName, metricSamples, availableTags);
    }

    internal (MetricsCollection<IList<MetricSample>> Samples, MetricsCollection<IList<MetricTag>> Tags) GetMetrics()
    {
        return _exporter.Export();
    }
}
