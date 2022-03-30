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
using System.Collections.Immutable;
using System.Linq;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.DirectIngestion;
using Wavefront.SDK.CSharp.Entities.Histograms;

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
    public class WavefrontMetricsExporter : IMetricsExporter
    {
        private readonly ILogger<WavefrontMetricsExporter> _logger;
        private WavefrontDirectIngestionClient _wavefrontSender;
        private WavefrontExporterOptions _options;

        internal WavefrontExporterOptions Options => _options;

        public WavefrontMetricsExporter(IWavefrontExporterOptions options, ILogger<WavefrontMetricsExporter> logger)
        {
            _options = options as WavefrontExporterOptions ?? throw new ArgumentNullException(nameof(options));
            var token = _options.ApiToken ?? throw new ArgumentNullException(nameof(_options.ApiToken));

            _logger = logger;
            _wavefrontSender = new WavefrontDirectIngestionClient.Builder(_options.Uri, token)
                                .MaxQueueSize(_options.MaxQueueSize)
                                .BatchSize(_options.BatchSize)
                                .FlushIntervalSeconds(_options.Step / 1000)
                                .Build();
        }

        public WavefrontMetricsExporter(Func<int, bool> collect)
        {
            Collect = collect;
        }

        public override System.Func<int, bool> Collect
        {
            get;
            set;
        }

        internal override System.Func<Batch<Metric>, ExportResult> OnExport
        {
            get;
            set;
        }

        public override ExportResult Export(in Batch<Metric> batch)
        {
            _logger.LogTrace("Calling export");
            foreach (var metric in batch)
            {
                bool isLong = ((int)metric.MetricType & 0b_0000_1111) == 0x0a; // I8 : signed 8 byte integer
                bool isSum = metric.MetricType.IsSum();
                var rand = new Random().NextDouble() * 100;

                var timestamp2 = DateTimeUtils.UnixTimeMilliseconds(DateTime.UtcNow);
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

                            _wavefrontSender.SendMetric(metric.Name.ToLower(), doubleValue, timestamp2, _options.Source, tags);
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
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending metrics to wavefront: " + ex.Message);
                }
            }

            return ExportResult.Success;
        }

        internal override ICollectionResponse GetCollectionResponse(Batch<Metric> metrics = default)
        {
            throw new System.NotImplementedException();
        }

        internal override ICollectionResponse GetCollectionResponse(ICollectionResponse collectionResponse, System.DateTime updatedTime)
        {
            throw new System.NotImplementedException();
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
