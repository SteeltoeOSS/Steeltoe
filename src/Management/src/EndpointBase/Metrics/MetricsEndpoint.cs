// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>, IMetricsEndpoint
    {
        private readonly SteeltoeExporter _exporter;
      //  private readonly OpenTelemetryMetrics _metrics;// Temporarily force creation
        private readonly ILogger<MetricsEndpoint> _logger;

        public MetricsEndpoint(IMetricsEndpointOptions options, SteeltoeExporter exporter, ILogger<MetricsEndpoint> logger = null)
            : base(options)
        {
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        //    _metrics = metrics;
            _logger = logger;
        }

        public new IMetricsEndpointOptions Options => options as IMetricsEndpointOptions;

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

            var valueSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.VALUE);
            if (valueSamples.Any())
            {
                var sample = valueSamples.Aggregate(SumAggregator);
                sampleList.Add(new MetricSample(MetricStatistic.VALUE, sample.Value / valueSamples.Count(), sample.Tags));
            }

            var totalSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TOTAL);
            if (totalSamples.Any())
            {
                sampleList.Add(totalSamples.Aggregate(SumAggregator));
            }

            var totalTimeSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.TOTAL_TIME);
            if (totalTimeSamples.Any())
            {
                sampleList.Add(totalTimeSamples.Aggregate(SumAggregator));
            }

            var countSamples = filtered.Where(sample => sample.Statistic == MetricStatistic.COUNT);
            if (countSamples.Any())
            {
                sampleList.Add(countSamples.Aggregate(SumAggregator));
            }

            return sampleList;
        }

        protected internal MetricsResponse GetMetric(MetricsRequest request, List<MetricSample> measurements, List<MetricTag> availTags)
        {
            return new MetricsResponse(request.MetricName, measurements, availTags);
        }

        //TODO: Move metrics Types to OpentelemetryBase
        protected internal void GetMetricsCollection(out MetricsCollection<List<MetricSample>> measurements, out MetricsCollection<List<MetricTag>> availTags) => _exporter.GetMetricsCollection(out measurements, out availTags);
        
           
#pragma warning restore CS0618 // Type or member is obsolete

    }
}