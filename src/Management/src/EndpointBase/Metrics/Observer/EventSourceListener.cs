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

    internal class EventSourceListener : EventListener
    {
        protected ConcurrentDictionary<string,MeasureMetric<double>> MeasureMetrics = new ConcurrentDictionary<string, MeasureMetric<double>>();
        
        private string _eventSourceName;
        private EventKeywords _eventKeywords;
        private IEnumerable<string> _allowedEvents;
        private readonly IStats _stats;
        private readonly ILogger<EventCounterListener> _logger;



        internal EventSourceListener(IStats stats, string eventSourceName, EventKeywords eventKeywords,  IEnumerable<string> allowedEvents, ILogger<EventCounterListener> logger=null)
        {
            _eventSourceName = eventSourceName;
            _eventKeywords = eventKeywords;
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _allowedEvents = allowedEvents ?? throw new ArgumentNullException(nameof(allowedEvents));
            _logger = logger;
        }

        protected Meter Meter => _stats.Meter;

        // private HashSet<string> Names = new HashSet<string>();
        /// <summary>
        /// Processes a new EventSource event.
        /// </summary>
        /// <param name="eventData">Event to process.</param>
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
                    ExtractAndRecordMetric(eventData.EventSource.Name, eventData.PayloadNames, eventData.Payload, GetLabelSet(eventData.EventName));
                    RecordAdditionalMetrics(eventData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        protected virtual void RecordAdditionalMetrics(EventWrittenEventArgs eventData)
        {
            // Do nothing unless overridden
        }

        protected IDictionary<string, string> GetLabelSet(string eventName)
        {
            return eventName switch
            {
                var en when eventName.StartsWith("IOThread", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, string> { { "kind", "completionPort" } },
                var en when eventName.StartsWith("ThreadPoolWorker", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, string> { { "kind", "worker" } },
                var en when eventName.StartsWith("Heap", StringComparison.OrdinalIgnoreCase) =>
                new Dictionary<string, string> { { "kind", "memory" } },
                _ => new Dictionary<string, string>()
            };
        }

        protected virtual void ExtractAndRecordMetric(string eventSourceName, IEnumerable<string> payLoadNames, IEnumerable<object> payload, IDictionary<string,string> _labels)
        {
            MeasureMetric<double> currentMetric = null;

            using var nameEnumerator = payLoadNames.GetEnumerator();
            using var payloadEnumerator = payload.GetEnumerator();

            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();

                var metricName = $"{eventSourceName}.{nameEnumerator.Current}";
                if (!MeasureMetrics.ContainsKey(metricName))
                {
                    currentMetric = MeasureMetrics.GetOrAddEx(
                        metricName,
                        (name) => _stats.Meter.CreateDoubleMeasure(name));
                }

                var actualValue = Convert.ToDouble(payloadEnumerator.Current, CultureInfo.InvariantCulture);
                currentMetric?.Record(default(SpanContext), actualValue, _labels.ToList());
            }

        }

        /// <summary>
        /// Processes notifications about new EventSource creation.
        /// </summary>
        /// <param name="eventSource">EventSource instance.</param>
        /// <remarks>When an instance of an EventListener is created, it will immediately receive notifications about all EventSources already existing in the AppDomain.
        /// Then, as new EventSources are created, the EventListener will receive notifications about them.</remarks>
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            if (eventSource.Name == _eventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose, _eventKeywords);
            }

            Console.WriteLine("Event Source name : " + eventSource.Name);
        }

    }
}