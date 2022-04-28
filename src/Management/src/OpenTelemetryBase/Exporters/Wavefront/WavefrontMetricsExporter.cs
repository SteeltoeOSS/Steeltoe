// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using Wavefront.SDK.CSharp.DirectIngestion;
using Wavefront.SDK.CSharp.Entities.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
    public class WavefrontMetricsExporter : BaseExporter<Metric>
    {
        private readonly ILogger<WavefrontMetricsExporter> _logger;
        private IWavefrontMetricSender _wavefrontSender;
        private WavefrontExporterOptions _options;

        internal WavefrontExporterOptions Options => _options;

        public WavefrontMetricsExporter(IWavefrontExporterOptions options, ILogger<WavefrontMetricsExporter> logger)
        {
            _options = options as WavefrontExporterOptions ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            var token = string.Empty;
            var uri = _options.Uri;
            if (_options.Uri.StartsWith("proxy://"))
            {
                uri = "http" + _options.Uri.Substring("proxy".Length); // Proxy reporting is now http on newer proxies.
            }
            else
            {
                // Token is required for Direct Ingestion
                token = _options.ApiToken ?? throw new ArgumentNullException(nameof(_options.ApiToken));
            }

            var flushInterval = Math.Max(_options.Step / 1000, 1); // Minimum of 1 second

            _wavefrontSender = new WavefrontDirectIngestionClient.Builder(uri, token)
                                    .MaxQueueSize(_options.MaxQueueSize)
                                    .BatchSize(_options.BatchSize)
                                    .FlushIntervalSeconds(flushInterval)
                                    .Build();
        }

        public override ExportResult Export(in Batch<Metric> batch)
        {
            int metricCount = 0;
            foreach (var metric in batch)
            {
                bool isLong = ((int)metric.MetricType & 0b_0000_1111) == 0x0a; // I8 : signed 8 byte integer
                bool isSum = metric.MetricType.IsSum();

                try
                {
                    if (!metric.MetricType.IsHistogram())
                    {
                        foreach (ref readonly var metricPoint in metric.GetMetricPoints())
                        {
                            var timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();
                            double doubleValue;
                            if (isLong)
                            {
                                doubleValue = isSum ? metricPoint.GetSumLong() : metricPoint.GetGaugeLastValueLong();
                            }
                            else
                            {
                                doubleValue = isSum ? metricPoint.GetSumDouble() : metricPoint.GetGaugeLastValueDouble();
                            }

                            var tags = GetTags(metricPoint.Tags);

                            _wavefrontSender.SendMetric(metric.Name.ToLower(), doubleValue, timestamp, _options.Source, tags);
                            metricCount++;
                        }
                    }
                    else
                    {
                        foreach (ref readonly var metricPoint in metric.GetMetricPoints())
                        {
                            var timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();

                            // TODO: Setup custom aggregations to compute distributions
                            var tags = GetTags(metricPoint.Tags);

                            _wavefrontSender.SendMetric(metric.Name.ToLower() + "_count", metricPoint.GetHistogramCount(), timestamp, _options.Source, tags);
                            _wavefrontSender.SendMetric(metric.Name.ToLower() + "_sum", metricPoint.GetHistogramSum(), timestamp, _options.Source, tags);
                            metricCount += 2;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending metrics to wavefront: " + ex.Message);
                }
            }

            _logger?.LogTrace($"Exported {metricCount} metrics to {_options.Uri}");
            return ExportResult.Success;
        }

        private IDictionary<string, string> GetTags(ReadOnlyTagCollection inputTags)
        {
            var tags = inputTags.AsDictionary();
            tags.Add("application", _options.Name.ToLower());
            tags.Add("service", _options.Service.ToLower());
            tags.Add("component", "wavefront-metrics-exporter");
            return tags;
        }
    }
}
