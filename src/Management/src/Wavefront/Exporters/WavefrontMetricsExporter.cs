// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Wavefront.SDK.CSharp.DirectIngestion;
using Wavefront.SDK.CSharp.Entities.Metrics;

#pragma warning disable S4040 // Strings should be normalized to uppercase

namespace Steeltoe.Management.Wavefront.Exporters;

public sealed class WavefrontMetricsExporter : BaseExporter<Metric>
{
    private readonly ILogger<WavefrontMetricsExporter> _logger;
    private readonly IWavefrontMetricSender _wavefrontSender;

    internal WavefrontExporterOptions Options { get; }

    public WavefrontMetricsExporter(WavefrontExporterOptions options, ILogger<WavefrontMetricsExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        Options = options;
        _logger = logger;

        string token = string.Empty;
        string? uri = Options.Uri;

        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException("management:metrics:export:wavefront:uri cannot be null or empty");
        }

        if (uri.StartsWith("proxy://", StringComparison.Ordinal))
        {
            uri = $"http{uri.Substring("proxy".Length)}"; // Proxy reporting is now http on newer proxies.
        }
        else
        {
            // Token is required for Direct Ingestion
            token = Options.ApiToken ?? throw new ArgumentException($"{nameof(options.ApiToken)} in {nameof(options)} must be provided.", nameof(options));
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

                        IDictionary<string, string?> tags = GetTags(metricPoint.Tags);

                        _wavefrontSender.SendMetric(metric.Name.ToLowerInvariant(), doubleValue, timestamp, Options.Source, tags);

                        metricCount++;
                    }
                }
                else
                {
                    foreach (ref readonly MetricPoint metricPoint in metric.GetMetricPoints())
                    {
                        long timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();

                        IDictionary<string, string?> tags = GetTags(metricPoint.Tags);

                        _wavefrontSender.SendMetric($"{metric.Name.ToLowerInvariant()}_count", metricPoint.GetHistogramCount(), timestamp, Options.Source,
                            tags);

                        _wavefrontSender.SendMetric($"{metric.Name.ToLowerInvariant()}_sum", metricPoint.GetHistogramSum(), timestamp, Options.Source, tags);

                        metricCount += 2;
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error sending metrics to wavefront");
            }
        }

        _logger.LogTrace("Exported {MetricCount} metrics to {Uri}", metricCount, Options.Uri);
        return ExportResult.Success;
    }

    private IDictionary<string, string?> GetTags(ReadOnlyTagCollection inputTags)
    {
        IDictionary<string, string?> tags = inputTags.AsDictionary();

        if (Options.Name != null)
        {
            tags.Add("application", Options.Name.ToLowerInvariant());
        }

        if (Options.Service != null)
        {
            tags.Add("service", Options.Service.ToLowerInvariant());
        }

        tags.Add("component", "wavefront-metrics-exporter");
        return tags;
    }
}
