// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class GCEventsListener : EventSourceListener
    {
        private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const string GCHeapStats = "GCHeapStats_V1";
        private const EventKeywords GCEventsKeywords = (EventKeywords)0x1;
        private const string GENERATION_TAGVALUE_NAME = "gen";

        private static string[] _ignorePayloadNames = new string[]
        {
                "ClrInstanceID"
        };

        private readonly string generationKey = "generation";
        private readonly ILogger<EventSourceListener> _logger;
        private readonly MeasureMetric<long> collectionCount;
        private readonly MeasureMetric<long> memoryUsed;
        private readonly Dictionary<string, string> memoryLabels = new Dictionary<string, string>() { { "area", "heap" } };

        private List<long> previousCollectionCounts = null;

        public GCEventsListener(IStats stats, ILogger<EventSourceListener> logger = null)
            : base(stats)
        {
            _logger = logger;
            memoryUsed = Meter.CreateInt64Measure("clr.memory.used");
            collectionCount = Meter.CreateInt64Measure("clr.gc.collections");
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            try
            {
                if (eventData.EventName.Equals(GCHeapStats, StringComparison.InvariantCulture))
                {
                    ExtractAndRecordMetric(EventSourceName, eventData, memoryLabels, _ignorePayloadNames);
                    RecordAdditionalMetrics(eventData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose, GCEventsKeywords);
            }
        }

        private void RecordAdditionalMetrics(EventWrittenEventArgs eventData)
        {
            long totalMemory = GC.GetTotalMemory(false);
            memoryUsed.Record(default(SpanContext), totalMemory, memoryLabels);
            List<long> counts = new List<long>(GC.MaxGeneration);
            for (int i = 0; i < GC.MaxGeneration; i++)
            {
                var count = (long)GC.CollectionCount(i);
                counts.Add(count);
                if (previousCollectionCounts != null && i < previousCollectionCounts.Count &&
                    previousCollectionCounts[i] <= count)
                {
                    count -= previousCollectionCounts[i];
                }

                var genKeylabelSet = new List<KeyValuePair<string, string>>()
                    { new KeyValuePair<string, string>(generationKey, GENERATION_TAGVALUE_NAME + i) };
                collectionCount.Record(default(SpanContext), count, genKeylabelSet);
            }

            previousCollectionCounts = counts;
        }
    }
}