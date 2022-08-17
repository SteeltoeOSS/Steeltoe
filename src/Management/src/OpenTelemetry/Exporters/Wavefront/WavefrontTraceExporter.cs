// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.DirectIngestion;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

/// <summary>
/// Exporter to send spans and traces to Wavefront from OpenTelemetry.
/// </summary>
public class WavefrontTraceExporter : BaseExporter<Activity>
{
    private readonly ILogger<WavefrontTraceExporter> _logger;
    private readonly WavefrontDirectIngestionClient _wavefrontSender;
    private readonly WavefrontExporterOptions _options;

    public WavefrontTraceExporter(IWavefrontExporterOptions options, ILogger<WavefrontTraceExporter> logger)
    {
        ArgumentGuard.NotNull(options);

        if (options is not WavefrontExporterOptions exporterOptions)
        {
            throw new ArgumentException($"Options must be convertible to {nameof(WavefrontExporterOptions)}.", nameof(options));
        }

        _options = exporterOptions;

        string token = string.Empty;
        string uri = _options.Uri;

        if (_options.Uri.StartsWith("proxy://"))
        {
            uri = $"http{_options.Uri.Substring("proxy".Length)}"; // Proxy reporting is now http on newer proxies.
        }
        else
        {
            // Token is required for Direct Ingestion
            token = _options.ApiToken ??
                throw new ArgumentException($"{nameof(exporterOptions.ApiToken)} in {nameof(options)} must be provided.", nameof(options));
        }

        int flushInterval = Math.Max(_options.Step / 1000, 1); // Minimum of 1 second

        _wavefrontSender = new WavefrontDirectIngestionClient.Builder(uri, token).MaxQueueSize(_options.MaxQueueSize).BatchSize(_options.BatchSize)
            .FlushIntervalSeconds(flushInterval).Build();

        _logger = logger;
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        int spanCount = 0;

        foreach (Activity activity in batch)
        {
            try
            {
                if (activity.Tags.Any(t => t.Key == "http.url" && !t.Value.Contains(_options.Uri)))
                {
                    _wavefrontSender.SendSpan(activity.OperationName, DateTimeUtils.UnixTimeMilliseconds(activity.StartTimeUtc), activity.Duration.Milliseconds,
                        _options.Source, Guid.Parse(activity.TraceId.ToString()), FromActivitySpanId(activity.SpanId), new List<Guid>
                        {
                            FromActivitySpanId(activity.ParentSpanId)
                        }, null, GetTags(activity.Tags), null);

                    spanCount++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending metrics to wavefront: {message}", ex.Message);
            }
        }

        _logger?.LogTrace($"Exported {spanCount} spans to {_options.Uri}");
        return ExportResult.Success;
    }

    private IList<KeyValuePair<string, string>> GetTags(IEnumerable<KeyValuePair<string, string>> inputTags)
    {
        List<KeyValuePair<string, string>> tags = inputTags.ToList();
        tags.Add(new KeyValuePair<string, string>("application", _options.Name.ToLower()));
        tags.Add(new KeyValuePair<string, string>("service", _options.Service.ToLower()));
        tags.Add(new KeyValuePair<string, string>("component", "wavefront-trace-exporter"));
        return tags;
    }

    private Guid FromActivitySpanId(ActivitySpanId spanId)
    {
        return Guid.Parse($"0000000000000000{spanId}");
    }
}
