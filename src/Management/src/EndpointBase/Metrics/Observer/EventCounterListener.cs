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
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Stats;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{

    public class EventCounterListener : EventListener
    {
        private const string EventSourceName = "System.Runtime";
       
        private readonly IStats _stats;
        private readonly ILogger<EventCounterListener> _logger;
        private readonly MeasureMetric<long> activeThreads;
        private readonly MeasureMetric<long> availThreads;
        private readonly MeasureMetric<long> collectionCount;
        private readonly MeasureMetric<long> memoryUsed;
        

        private ConcurrentDictionary<string,MeasureMetric<double>> measureMetrics = new ConcurrentDictionary<string, MeasureMetric<double>>();
       // private readonly string generationKey = "generation";
       // private const string GENERATION_TAGVALUE_NAME = "gen";
        // private readonly IEnumerable<KeyValuePair<string, string>> memoryLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("area", "heap") };
        // private readonly IEnumerable<KeyValuePair<string, string>> threadPoolWorkerLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("kind", "worker") };
        // private readonly IEnumerable<KeyValuePair<string, string>> threadPoolComPortLabels = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("kind", "completionPort") };


        public EventCounterListener(IStats stats, ILogger<EventCounterListener> logger=null)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _logger = logger;

            // memoryUsed = _stats.Meter.CreateInt64Measure("clr.memory.used");
            // collectionCount = _stats.Meter.CreateInt64Measure("clr.gc.collections");
            // activeThreads = _stats.Meter.CreateInt64Measure("clr.threadpool.active");
            // availThreads = _stats.Meter.CreateInt64Measure("clr.threadpool.avail");
        }

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
            foreach (IDictionary<string, object> payload in eventData.Payload)
            {
                ExtractAndRecordMetric(eventData.EventSource.Name, payload);
            }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        private void ExtractAndRecordMetric(string eventSourceName, IDictionary<string, object> eventPayload)
        {

            var metricName = string.Empty;
            var actualValue = 0.0;
            var counterName = string.Empty;
            var labelSet = new List<KeyValuePair<string, string>>();
            MeasureMetric<double> currentMetric = null;
            foreach (var payload in eventPayload)
            {
                var key = payload.Key;
                switch (key)
                {
                    case var kn when key.Equals("Name", StringComparison.OrdinalIgnoreCase):
                        counterName = payload.Value.ToString();
                        break;
                    case var kn when key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase):
                        var counterDisplayName = payload.Value.ToString();
                        labelSet.Add(KeyValuePair.Create("DisplayName", counterDisplayName));
                        break;
                    case var kn when key.Equals("DisplayUnits", StringComparison.OrdinalIgnoreCase):
                        var counterDisplayUnit = payload.Value.ToString();
                        labelSet.Add(KeyValuePair.Create("DisplayUnits", counterDisplayUnit));
                        break;
                    case var kn when key.Equals("Mean", StringComparison.OrdinalIgnoreCase):
                        actualValue = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                        break;
                    case var kn when key.Equals("Increment", StringComparison.OrdinalIgnoreCase):
                        actualValue = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                        break;
                    case var kn when key.Equals("IntervalSec", StringComparison.OrdinalIgnoreCase):
                        var actualInterval = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                        labelSet.Add(KeyValuePair.Create("IntervalSec",
                            actualInterval.ToString(CultureInfo.InvariantCulture)));
                        break;
                    case var kn when key.Equals("Count", StringComparison.OrdinalIgnoreCase):
                        var actualCount = Convert.ToInt32(payload.Value, CultureInfo.InvariantCulture);
                        labelSet.Add(KeyValuePair.Create("Count",
                            actualCount.ToString(CultureInfo.InvariantCulture)));
                        break;
                    case var kn when key.Equals("Metadata", StringComparison.OrdinalIgnoreCase):
                        var metadata = payload.Value.ToString();
                        if (!string.IsNullOrEmpty(metadata))
                        {
                            var keyValuePairStrings = metadata.Split(',');
                            foreach (var keyValuePairString in keyValuePairStrings)
                            {
                                var keyValuePair = keyValuePairString.Split(':');
                                labelSet.Add(KeyValuePair.Create(keyValuePair[0], keyValuePair[1]));
                            }
                        }

                        break;
                }

                metricName = eventSourceName + "." + counterName;
            }

            if (metricName.EndsWith("gc-heap-size"))
            {

                if (!measureMetrics.ContainsKey(metricName))
                {
                    currentMetric = measureMetrics.GetOrAddEx(metricName,
                        (name) => _stats.Meter.CreateDoubleMeasure($"{EventSourceName}.{name}"));
                }



                currentMetric?.Record(default(SpanContext), actualValue, LabelSet.BlankLabelSet);
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

            // const EventKeywords keywords = (EventKeywords)(Keywords.GC & Keywords.Threadpool);
            if (eventSource.Name == "System.Runtime")
            {
                var refreshInterval = new Dictionary<string, string>() {{"EventCounterIntervalSec", "1"}};
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, refreshInterval);
            }

            Console.WriteLine("Event Source name : " + eventSource.Name);
        }

    }
}