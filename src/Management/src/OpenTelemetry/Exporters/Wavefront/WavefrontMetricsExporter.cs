// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Wavefront.SDK.CSharp.DirectIngestion;
using Wavefront.SDK.CSharp.Entities.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

public class WavefrontMetricsExporter : BaseExporter<Metric>
{
    private readonly ILogger<WavefrontMetricsExporter> _logger;
    private readonly IWavefrontMetricSender _wavefrontSender;

    internal WavefrontExporterOptions Options { get; }

    public WavefrontMetricsExporter(IWavefrontExporterOptions options, ILogger<WavefrontMetricsExporter> logger)
    {
        ArgumentGuard.NotNull(options);

        if (options is not WavefrontExporterOptions exporterOptions)
        {
            throw new ArgumentException($"Options must be convertible to {nameof(WavefrontExporterOptions)}.", nameof(options));
        }

        Options = exporterOptions;
        _logger = logger;

        string token = string.Empty;
        string uri = Options.Uri;

        if (Options.Uri.StartsWith("proxy://"))
        {
            uri = $"http{Options.Uri.Substring("proxy".Length)}"; // Proxy reporting is now http on newer proxies.
        }
        else
        {
            // Token is required for Direct Ingestion
            token = Options.ApiToken ??
                throw new ArgumentException($"{nameof(exporterOptions.ApiToken)} in {nameof(options)} must be provided.", nameof(options));
        }

        int flushInterval = Math.Max(Options.Step / 1000, 1); // Minimum of 1 second

        _wavefrontSender = new WavefrontDirectIngestionClient.Builder(uri, token).MaxQueueSize(Options.MaxQueueSize).BatchSize(Options.BatchSize)
            .FlushIntervalSeconds(flushInterval).Build();
    }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        int metricCount = 0;

        foreach (Metric metric in batch)
        {
            bool isLong = ((int)metric.MetricType & 0b_0000_1111) == 0x0a; // I8 : signed 8 byte integer
            bool isSum = metric.MetricType.IsSum();

            try
            {
                if (!metric.MetricType.IsHistogram())
                {
                    foreach (ref readonly MetricPoint metricPoint in metric.GetMetricPoints())
                    {
                        long timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();
                        double doubleValue;

                        if (isLong)
                        {
                            doubleValue = isSum ? metricPoint.GetSumLong() : metricPoint.GetGaugeLastValueLong();
                        }
                        else
                        {
                            doubleValue = isSum ? metricPoint.GetSumDouble() : metricPoint.GetGaugeLastValueDouble();
                        }

                        IDictionary<string, string> tags = GetTags(metricPoint.Tags);

                        _wavefrontSender.SendMetric(metric.Name.ToLower(), doubleValue, timestamp, Options.Source, tags);
                        metricCount++;
                    }
                }
                else
                {
                    foreach (ref readonly MetricPoint metricPoint in metric.GetMetricPoints())
                    {
                        long timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();

                        // TODO: Setup custom aggregations to compute distributions
                        IDictionary<string, string> tags = GetTags(metricPoint.Tags);

                        _wavefrontSender.SendMetric($"{metric.Name.ToLower()}_count", metricPoint.GetHistogramCount(), timestamp, Options.Source, tags);
                        _wavefrontSender.SendMetric($"{metric.Name.ToLower()}_sum", metricPoint.GetHistogramSum(), timestamp, Options.Source, tags);
                        metricCount += 2;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending metrics to wavefront: {message}", ex.Message);
            }
        }

        _logger?.LogTrace($"Exported {metricCount} metrics to {Options.Uri}");
        return ExportResult.Success;
    }

    private IDictionary<string, string> GetTags(ReadOnlyTagCollection inputTags)
    {
        IDictionary<string, string> tags = inputTags.AsDictionary();
        tags.Add("application", Options.Name.ToLower());
        tags.Add("service", Options.Service.ToLower());
        tags.Add("component", "wavefront-metrics-exporter");
        return tags;
    }
}
