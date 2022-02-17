// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    /// <summary>
    /// This EventSourceListener listens on the following events:
    /// ThreadPoolWorkerThreadStart, ThreadPoolWorkerThreadWait, ThreadPoolWorkerThreadStop,
    /// IOThreadCreate_V1, IOThreadRetire_V1, IOThreadUnretire_V1, IOThreadTerminate
    /// And Records the following values:
    /// ActiveWorkerThreadCount - UInt32 - Number of worker threads available to process work, including those that are already processing work.
    /// RetiredWorkerThreadCount - UInt32 - Number of worker threads that are not available to process work, but that are being held in reserve in case more threads are needed later.
    /// </summary>
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class ThreadPoolEventsListener : EventSourceListener
    {
        private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const EventKeywords ThreadPoolEvents = (EventKeywords)0x10000;

        private static string[] _allowedEvents = new string[]
        {
            "ThreadPoolWorkerThreadStart",
            "ThreadPoolWorkerThreadWait",
            "ThreadPoolWorkerThreadStop",
            "IOThreadCreate_V1",
            "IOThreadRetire_V1",
            "IOThreadUnretire_V1",
            "IOThreadTerminate"
        };

        private static string[] _ignorePayloadNames = new string[]
        {
            "ClrInstanceID"
        };

        private readonly ILogger<EventSourceListener> _logger;
        private readonly Counter<long> _availableThreads;

        public ThreadPoolEventsListener(ILogger<EventSourceListener> logger = null)
            : base()
        {
            _logger = logger;
            _availableThreads = OpenTelemetryMetrics.Meter.CreateCounter<long>($"clr.threadpool.available");
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

        protected IDictionary<string, object> GetLabelSet(string eventName)
        {
            return eventName switch
            {
                _ when eventName.StartsWith("IOThread", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, object> { { "kind", "completionPort" } },
                _ when eventName.StartsWith("ThreadPoolWorker", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, object> { { "kind", "worker" } },
                _ => new Dictionary<string, object>()
            };
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                SafelyEnableEvents(eventSource, EventLevel.Verbose, ThreadPoolEvents);
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
                    _availableThreads.Add(available, GetLabelSet(eventData.EventName).AsReadonlySpan());
                }
            }
        }
    }
}