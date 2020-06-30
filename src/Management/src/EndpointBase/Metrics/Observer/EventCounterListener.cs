// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class EventCounterListener : EventListener
    {
        private readonly IStats _stats;
        private readonly ILogger<EventCounterListener> _logger;
        private readonly string _eventSourceName = "System.Runtime";
        private readonly string _eventName = "EventCounters";

        private ConcurrentDictionary<string, MeasureMetric<double>> doubleMeasureMetrics = new ConcurrentDictionary<string, MeasureMetric<double>>();
        private ConcurrentDictionary<string, MeasureMetric<long>> longMeasureMetrics = new ConcurrentDictionary<string, MeasureMetric<long>>();

        public EventCounterListener(IStats stats, ILogger<EventCounterListener> logger = null)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _logger = logger;
        }

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
                if (eventData.EventName.Equals(_eventName, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (IDictionary<string, object> payload in eventData.Payload)
                    {
                        ExtractAndRecordMetric(eventData.EventSource.Name, payload);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            if (_eventSourceName.Equals(eventSource.Name, StringComparison.OrdinalIgnoreCase))
            {
                var refreshInterval = new Dictionary<string, string>() { { "EventCounterIntervalSec", "1" } };
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, refreshInterval);
            }
        }

        private void ExtractAndRecordMetric(string eventSourceName, IDictionary<string, object> eventPayload)
        {
            var metricName = string.Empty;
            double? doubleValue = null;
            long? longValue = null;
            var counterName = string.Empty;
            var labelSet = new List<KeyValuePair<string, string>>();
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
                        doubleValue = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                        break;
                    case var kn when key.Equals("Increment", StringComparison.OrdinalIgnoreCase):
                        longValue = Convert.ToInt64(payload.Value, CultureInfo.InvariantCulture);
                        break;
                    case var kn when key.Equals("IntervalSec", StringComparison.OrdinalIgnoreCase):
                        var actualInterval = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                        break;
                    case var kn when key.Equals("Count", StringComparison.OrdinalIgnoreCase):
                        longValue = Convert.ToInt64(payload.Value, CultureInfo.InvariantCulture);
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

            if (doubleValue.HasValue)
            {
                var doubleMetric = doubleMeasureMetrics.GetOrAddEx(
                    metricName,
                    (name) => _stats.Meter.CreateDoubleMeasure($"{name}"));
                doubleMetric.Record(default(SpanContext), doubleValue.Value, labelSet);
            }
            else if (longValue.HasValue)
            {
                var longMetric = longMeasureMetrics.GetOrAddEx(
                    metricName,
                    (name) => _stats.Meter.CreateInt64Measure($"{name}"));
                longMetric.Record(default(SpanContext), longValue.Value, labelSet);
            }
        }
    }
}