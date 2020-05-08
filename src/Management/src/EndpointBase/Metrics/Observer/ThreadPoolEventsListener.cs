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

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class ThreadpoolEventsListener : EventSourceListener
    {
        private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const EventKeywords ThreadPoolEvents = (EventKeywords)0x10000;

        private static readonly string[] _allowedEvents = new string[]
        {
            "ThreadPoolWorkerThreadStart",
            "ThreadPoolWorkerThreadWait",
            "ThreadPoolWorkerThreadStop",
            "IOThreadCreate_V1",
            "IOThreadRetire_V1",
            "IOThreadUnretire_V1",
            "IOThreadTerminate"
        };

        private static readonly string[] _ignorePayloadNames = new string[]
        {
            "ClrInstanceID"
        };

        private readonly ILogger<EventSourceListener> _logger;
        private readonly MeasureMetric<long> availableThreads;

        public ThreadpoolEventsListener(IStats stats, ILogger<EventSourceListener> logger = null)
            : base(stats)
        {
            _logger = logger;
            availableThreads = Meter.CreateInt64Measure($"clr.threadpool.available");
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            try
            {
                if (_allowedEvents.Any(e => e.Equals(eventData.EventName, StringComparison.InvariantCulture)))
                {
                    ExtractAndRecordMetric(EventSourceName, eventData, GetLabelSet(eventData.EventName), _ignorePayloadNames);
                    RecordAdditionalMetrics(eventData);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message);
            }
        }

        protected IDictionary<string, string> GetLabelSet(string eventName)
        {
            return eventName switch
            {
                var en when eventName.StartsWith("IOThread", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, string> { { "kind", "completionPort" } },
                var en when eventName.StartsWith("ThreadPoolWorker", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, string> { { "kind", "worker" } },
                _ => new Dictionary<string, string>()
            };
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose, ThreadPoolEvents);
            }
        }

        private void RecordAdditionalMetrics(EventWrittenEventArgs eventData)
        {
            ThreadPool.GetMaxThreads(out var maxWorker, out var maxComPort);
            using var nameEnumerator = eventData.PayloadNames.GetEnumerator();
            using var payloadEnumerator = eventData.Payload.GetEnumerator();

            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();

                if ((eventData.EventName.StartsWith("ThreadPoolWorker", StringComparison.OrdinalIgnoreCase)
                    && nameEnumerator.Current.Equals("ActiveWorkerThreadCount", StringComparison.OrdinalIgnoreCase))
                    || (eventData.EventName.StartsWith("IOThread", StringComparison.OrdinalIgnoreCase)
                    && nameEnumerator.Current.EndsWith("Count", StringComparison.OrdinalIgnoreCase)))
                {
                    var activeCount = Convert.ToInt64(payloadEnumerator.Current, CultureInfo.InvariantCulture);
                    var available = maxWorker - activeCount;
                    availableThreads.Record(default(SpanContext), available, GetLabelSet(eventData.EventName).ToList());
                }
            }
        }
    }
}