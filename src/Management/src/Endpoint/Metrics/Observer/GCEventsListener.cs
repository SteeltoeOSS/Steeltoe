// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

/// <summary>
/// Events are fired when garbage collection occurs.
/// </summary>
[Obsolete("Use CLRRuntimeSource Instead")]
public class GCEventsListener : EventSourceListener
{
    private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
    private const string GCHeapStats = "GCHeapStats_V1";
    private const string GCHeapStatsV2 = "GCHeapStats_V2";
    private const EventKeywords GCEventsKeywords = (EventKeywords)0x1;
    private const string GenerationTagValueName = "gen";
    private const string GenerationKey = "generation";

    private static readonly string[] IgnorePayloadNames =
    {
        "ClrInstanceID"
    };

    private readonly ILogger<EventSourceListener> _logger;
    private readonly Counter<long> _collectionCount;
    private readonly Counter<double> _memoryUsed;

    private readonly Dictionary<string, object> _memoryLabels = new()
    {
        { "area", "heap" }
    };

    private List<long> _previousCollectionCounts;

    public GCEventsListener(ILogger<EventSourceListener> logger = null)
    {
        _logger = logger;
        Meter meter = OpenTelemetryMetrics.Meter;
        _memoryUsed = meter.CreateCounter<double>("clr.memory.used");
        _collectionCount = meter.CreateCounter<long>("clr.gc.collections");
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        ArgumentGuard.NotNull(eventData);

        try
        {
            if (eventData.EventName.Equals(GCHeapStats, StringComparison.InvariantCulture) ||
                eventData.EventName.Equals(GCHeapStatsV2, StringComparison.InvariantCulture))
            {
                ExtractAndRecordMetric(EventSourceName, eventData, _memoryLabels, IgnorePayloadNames);
                RecordAdditionalMetrics(eventData);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == EventSourceName)
        {
            SafelyEnableEvents(eventSource, EventLevel.Verbose, GCEventsKeywords);
        }
    }

    private void RecordAdditionalMetrics(EventWrittenEventArgs eventData)
    {
        long totalMemory = GC.GetTotalMemory(false);
        _memoryUsed.Add(totalMemory, _memoryLabels.AsReadonlySpan());
        var counts = new List<long>(GC.MaxGeneration);

        for (int i = 0; i < GC.MaxGeneration; i++)
        {
            long count = GC.CollectionCount(i);
            counts.Add(count);

            if (_previousCollectionCounts != null && i < _previousCollectionCounts.Count && _previousCollectionCounts[i] <= count)
            {
                count -= _previousCollectionCounts[i];
            }

            var genKeyLabelSet = new List<KeyValuePair<string, object>>
            {
                new(GenerationKey, GenerationTagValueName + i)
            };

            _collectionCount.Add(count, genKeyLabelSet.AsReadonlySpan());
        }

        _previousCollectionCounts = counts;
    }
}
