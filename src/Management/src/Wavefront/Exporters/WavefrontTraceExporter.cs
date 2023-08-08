// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using Steeltoe.Common;
using Wavefront.SDK.CSharp.Common;
using Wavefront.SDK.CSharp.DirectIngestion;

#pragma warning disable S4040 // Strings should be normalized to uppercase

namespace Steeltoe.Management.Wavefront.Exporters;

/// <summary>
/// Exporter to send spans and traces to Wavefront from OpenTelemetry.
/// </summary>
public sealed class WavefrontTraceExporter : BaseExporter<Activity>
{
    private readonly ILogger<WavefrontTraceExporter> _logger;
    private readonly WavefrontDirectIngestionClient _wavefrontSender;
    private readonly WavefrontExporterOptions _options;

    public WavefrontTraceExporter(WavefrontExporterOptions options, ILogger<WavefrontTraceExporter> logger)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(logger);

        _options = options;

        string token = string.Empty;
        string? uri = _options.Uri;

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
            token = _options.ApiToken ?? throw new ArgumentException($"{nameof(options.ApiToken)} in {nameof(options)} must be provided.", nameof(options));
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
                if (!activity.Tags.Any(pair => pair.Key == "http.url" && pair.Value != null && pair.Value.Contains(_options.Uri!, StringComparison.Ordinal)))
                {
                    _wavefrontSender.SendSpan(activity.OperationName, DateTimeUtils.UnixTimeMilliseconds(activity.StartTimeUtc), activity.Duration.Milliseconds,
                        _options.Source, Guid.Parse(activity.TraceId.ToString()), FromActivitySpanId(activity.SpanId), new List<Guid>
                        {
                            FromActivitySpanId(activity.ParentSpanId)
                        }, null, GetTags(activity.Tags), null);

                    spanCount++;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error sending metrics to wavefront");
            }
        }

        _logger.LogTrace($"Exported {spanCount} spans to {_options.Uri}");
        return ExportResult.Success;
    }

    private IList<KeyValuePair<string, string?>> GetTags(IEnumerable<KeyValuePair<string, string?>> inputTags)
    {
        List<KeyValuePair<string, string?>> tags = inputTags.ToList();

        if (_options.Name != null)
        {
            tags.Add(new KeyValuePair<string, string?>("application", _options.Name.ToLowerInvariant()));
        }

        if (_options.Service != null)
        {
            tags.Add(new KeyValuePair<string, string?>("service", _options.Service.ToLowerInvariant()));
        }

        tags.Add(new KeyValuePair<string, string?>("component", "wavefront-trace-exporter"));

        return tags;
    }

    private Guid FromActivitySpanId(ActivitySpanId spanId)
    {
        return Guid.Parse($"0000000000000000{spanId}");
    }
}
