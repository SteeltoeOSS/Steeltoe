// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class GCEventsListener : EventSourceListener
    {
        private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const string GCHeapStats = "GCHeapStats_V1";
        private const string GCHeapStatsV2 = "GCHeapStats_V2";
        private const EventKeywords GCEventsKeywords = (EventKeywords)0x1;
        private const string GENERATION_TAGVALUE_NAME = "gen";

        private static string[] _ignorePayloadNames = new string[]
        {
                "ClrInstanceID"
        };

        private readonly string _generationKey = "generation";
        private readonly ILogger<EventSourceListener> _logger;
        private readonly MeasureMetric<long> _collectionCount;
        private readonly MeasureMetric<long> _memoryUsed;
        private readonly Dictionary<string, string> _memoryLabels = new () { { "area", "heap" } };

        private List<long> _previousCollectionCounts = null;

        public GCEventsListener(IStats stats, ILogger<EventSourceListener> logger = null)
            : base(stats)
        {
            _logger = logger;
            _memoryUsed = Meter.CreateInt64Measure("clr.memory.used");
            _collectionCount = Meter.CreateInt64Measure("clr.gc.collections");
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            try
            {
                if (eventData.EventName.Equals(GCHeapStats, StringComparison.InvariantCulture) || eventData.EventName.Equals(GCHeapStatsV2, StringComparison.InvariantCulture))
                {
                    ExtractAndRecordMetric(EventSourceName, eventData, _memoryLabels, _ignorePayloadNames);
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
            var totalMemory = GC.GetTotalMemory(false);
            _memoryUsed.Record(default, totalMemory, _memoryLabels);
            var counts = new List<long>(GC.MaxGeneration);
            for (var i = 0; i < GC.MaxGeneration; i++)
            {
                var count = (long)GC.CollectionCount(i);
                counts.Add(count);
                if (_previousCollectionCounts != null && i < _previousCollectionCounts.Count &&
                    _previousCollectionCounts[i] <= count)
                {
                    count -= _previousCollectionCounts[i];
                }

                var genKeylabelSet = new List<KeyValuePair<string, string>>()
                    { new KeyValuePair<string, string>(_generationKey, GENERATION_TAGVALUE_NAME + i) };
                _collectionCount.Record(default, count, genKeylabelSet);
            }

            _previousCollectionCounts = counts;
        }
    }
}