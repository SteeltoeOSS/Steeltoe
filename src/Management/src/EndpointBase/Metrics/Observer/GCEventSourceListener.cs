// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Stats;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{

    internal class GCEventsListener : EventSourceListener
    {
        private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const string GCHeapStats = "GCHeapStats_V1";
        private const EventKeywords GCEvents = (EventKeywords) 0x1;
        private readonly ILogger<EventCounterListener> _logger;
        private readonly MeasureMetric<long> collectionCount;
        private readonly MeasureMetric<long> memoryUsed;
        private List<long> previousCollectionCounts = null;

        private readonly string generationKey = "generation";
        private const string GENERATION_TAGVALUE_NAME = "gen";

        private readonly IEnumerable<KeyValuePair<string, string>> memoryLabels =
            new List<KeyValuePair<string, string>>() {new KeyValuePair<string, string>("area", "heap")};

        public GCEventsListener(IStats stats, ILogger<EventCounterListener> logger = null)
            : base(stats, EventSourceName, GCEvents, new[] { GCHeapStats }.ToList(), logger)
        {
            _logger = logger;

            memoryUsed = Meter.CreateInt64Measure("clr.memory.used");
            collectionCount = Meter.CreateInt64Measure("clr.gc.collections");
        }

        // protected override void OnEventWritten(EventWrittenEventArgs eventData)
        // {
        //     if (eventData == null)
        //     {
        //         throw new ArgumentNullException(nameof(eventData));
        //     }
        //
        //     try
        //     {
        //         base.OnEventWritten(eventData);
        //         if (eventData.EventName.Equals(GCHeapStats, StringComparison.InvariantCulture))
        //         {
        //             RecordAdditionalMetrics();
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex.Message);
        //     }
        // }

        protected override void RecordAdditionalMetrics(EventWrittenEventArgs eventData)
        {
            long totalMemory = GC.GetTotalMemory(false);
            memoryUsed.Record(default(SpanContext), totalMemory, memoryLabels);
            List<long> counts = new List<long>(GC.MaxGeneration);
            for (int i = 0; i < GC.MaxGeneration; i++)
            {
                var count = (long) GC.CollectionCount(i);
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