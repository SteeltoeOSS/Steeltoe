// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class EventSourceListener : EventListener
    {
        protected ConcurrentDictionary<string, Counter<long>> LongCounters { get; set; }

        protected ConcurrentDictionary<string, Counter<double>> DoubleCounters { get; set; }

        private readonly ILogger<EventSourceListener> _logger;

        internal EventSourceListener( ILogger<EventSourceListener> logger = null)
        {
            _logger = logger;
            LongCounters = new ConcurrentDictionary<string, Counter<long>>();
            DoubleCounters = new ConcurrentDictionary<string, Counter<double>>();
        }

        protected virtual void ExtractAndRecordMetric(
            string eventSourceName,
            EventWrittenEventArgs eventData,
            IDictionary<string, object> labels,
            string[] ignorePayloadNames = null,
            string[] counterNames = null)
        {
            var payloadNames = eventData.PayloadNames;
            var payload = eventData.Payload;

            var names = payloadNames.Where(name => ignorePayloadNames == null || !ignorePayloadNames.Contains(name)).ToList();

            var currentLabels = GetLabels(payload, names, labels);

            using var payloadEnumerator = payload.GetEnumerator();
            using var nameEnumerator = names.GetEnumerator();
            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();
                var metricName = $"{eventSourceName}.{eventData.EventName}.{nameEnumerator.Current}";
                RecordMetricsWithLabels(
                     metricName,
                     nameEnumerator.Current,
                     payloadEnumerator.Current,
                     currentLabels,
                     counterNames);
            }
        }

        protected void SafelyEnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
        {
            try
            {
                EnableEvents(eventSource, level, matchAnyKeyword);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message, ex);
            }
        }

        private IDictionary<string, object> GetLabels(
            IEnumerable<object> payload,
            IList<string> names,
            IDictionary<string, object> labels)
        {
            var nameEnumerator = names.GetEnumerator();
            var payloadEnumerator = payload.GetEnumerator();

            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();

                switch (payloadEnumerator.Current)
                {
                    case string strValue:
                        if (!labels.ContainsKey(nameEnumerator.Current))
                        {
                            labels.Add(nameEnumerator.Current, strValue);
                        }

                        break;
                }
            }

            return labels;
        }

        private void RecordMetricsWithLabels(
            string metricName,
            string payLoadName,
            object payloadValue,
            IDictionary<string, object> labels,
            string[] counterNames)
        {
            long? longValue = null;
            double? doubleValue = null;

            switch (payloadValue)
            {
                case string stringValue:
                    break;
                case short shortValue:
                    longValue = shortValue;
                    break;
                case int intValue:
                    longValue = intValue;
                    break;
                case uint unsignedInt:
                    longValue = unsignedInt;
                    break;
                case long lValue:
                    longValue = lValue;
                    break;
                case ulong ulValue:
                    longValue = (long)ulValue;
                    break;
                case double dValue:
                    doubleValue = dValue;
                    break;
                case bool boolValue:
                    longValue = Convert.ToInt64(boolValue);
                    break;
                default:
                    _logger?.LogDebug($"Unhandled type at {metricName} - {payloadValue.GetType()} - {payloadValue}");
                    break;
            }

            if (longValue.HasValue)
            {
                var currentMetric = LongCounters.GetOrAddEx(metricName, (name) => OpenTelemetryMetrics.Meter.CreateCounter<long>(name));
                currentMetric.Add(longValue.Value, labels.AsReadonlySpan());
            }
            else if (doubleValue.HasValue)
            {
                var currentMetric = DoubleCounters.GetOrAddEx(metricName, (name) => OpenTelemetryMetrics.Meter.CreateCounter<double>(name));
                currentMetric.Add(doubleValue.Value, labels.AsReadonlySpan());
            }
        }
    }
}
