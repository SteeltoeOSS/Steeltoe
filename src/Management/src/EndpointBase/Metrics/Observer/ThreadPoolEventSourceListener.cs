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
using System.Threading;
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

    internal class ThreadpoolEventSourceListener : EventSourceListener
    {
        private const string EventSourceName = "Microsoft-Windows-DotNETRuntime";
        private const EventKeywords ThreadPoolEvents = (EventKeywords)0x10000;

        private static string [] _allowedEvents = new string[]
        {
            "ThreadPoolWorkerThreadStart", "ThreadPoolWorkerThreadWait", "ThreadPoolWorkerThreadStop",
        };
        // private readonly IEnumerable<KeyValuePair<string, string>> threadPoolWorkerLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("kind", "worker") };
        // private readonly IEnumerable<KeyValuePair<string, string>> threadPoolComPortLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("kind", "completionPort") };

        private readonly ILogger<EventCounterListener> _logger;
        private readonly MeasureMetric<double> availableThreads;


        public ThreadpoolEventSourceListener(IStats stats, ILogger<EventCounterListener> logger = null)
            : base(stats, EventSourceName, ThreadPoolEvents, _allowedEvents.ToList(), logger)
        {
            _logger = logger;


            availableThreads = Meter.CreateDoubleMeasure($"{EventSourceName}.AvailableWorkerThreadCount");
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
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex.Message);
        //     }
        // }

        protected override void RecordAdditionalMetrics(EventWrittenEventArgs eventData)
        {
            ThreadPool.GetMaxThreads(out var maxWorker, out var maxComPort);
            using var nameEnumerator = eventData.PayloadNames.GetEnumerator();
            using var payloadEnumerator = eventData.Payload.GetEnumerator();


            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();

                if (eventData.EventName.StartsWith("ThreadPoolWorker", StringComparison.OrdinalIgnoreCase)
                    && nameEnumerator.Current.Equals("ActiveWorkerThreadCount", StringComparison.OrdinalIgnoreCase))
                {
                    var activeCount = Convert.ToDouble(payloadEnumerator.Current, CultureInfo.InvariantCulture);
                    var available = (double)maxWorker - activeCount;
                    availableThreads.Record(default(SpanContext), available, GetLabelSet(eventData.EventName).ToList());
                }

                if (eventData.EventName.StartsWith("IOThread", StringComparison.OrdinalIgnoreCase)
                    && nameEnumerator.Current.Equals("Count", StringComparison.OrdinalIgnoreCase))
                {
                    var activeCount = Convert.ToDouble(payloadEnumerator.Current, CultureInfo.InvariantCulture);
                    var available = (double)maxWorker - activeCount;
                    availableThreads.Record(default(SpanContext), available, GetLabelSet(eventData.EventName).ToList());
                }
            }

        }

    }
}