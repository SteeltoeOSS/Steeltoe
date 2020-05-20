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
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
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
                    longValue = boolValue ? 1 : 0;
                    break;
                default:
                    Console.WriteLine($"Unhandled type at {metricName} - {payloadValue.GetType()} - {payloadValue}");
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