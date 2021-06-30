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
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class EventSourceListener : EventListener
    {
        protected ConcurrentDictionary<string, MeasureMetric<long>> LongMeasureMetrics { get; set; }

        protected ConcurrentDictionary<string, MeasureMetric<double>> DoubleMeasureMetrics { get; set; }

        private readonly IStats _stats;
        private readonly ILogger<EventSourceListener> _logger;

        internal EventSourceListener(IStats stats, ILogger<EventSourceListener> logger = null)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _logger = logger;
            LongMeasureMetrics = new ConcurrentDictionary<string, MeasureMetric<long>>();
            DoubleMeasureMetrics = new ConcurrentDictionary<string, MeasureMetric<double>>();
        }

        protected Meter Meter => _stats.Meter;

        protected virtual void ExtractAndRecordMetric(
            string eventSourceName,
            EventWrittenEventArgs eventData,
            IDictionary<string, string> labels,
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

        private IDictionary<string, string> GetLabels(
            IEnumerable<object> payload,
            IList<string> names,
            IDictionary<string, string> labels)
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
            IDictionary<string, string> labels,
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
                var currentMetric = LongMeasureMetrics.GetOrAddEx(metricName, (name) => _stats.Meter.CreateInt64Measure(name));
                currentMetric.Record(default(SpanContext), longValue.Value, labels);
            }
            else if (doubleValue.HasValue)
            {
                var currentMetric = DoubleMeasureMetrics.GetOrAddEx(metricName, (name) => _stats.Meter.CreateDoubleMeasure(name));
                currentMetric.Record(default(SpanContext), doubleValue.Value, labels);
            }
        }
    }
}
