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
using OpenTelemetry.Metrics.Export;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Threading;

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

        protected virtual void ExtractAndRecordMetric(string eventSourceName, EventWrittenEventArgs eventData, IDictionary<string, string> labels, string[] ignorePayloadNames = null)
        {
            var payloadNames = eventData.PayloadNames;
            var payload = eventData.Payload;

            using var nameEnumerator = payloadNames.Where(name => ignorePayloadNames == null || !ignorePayloadNames.Contains(name)).GetEnumerator();
            using var payloadEnumerator = payload.GetEnumerator();

            var currentLabels = new Dictionary<string, string>(labels);
            var doubleValues = new Dictionary<string, double>();
            var longValues = new Dictionary<string, long>();
            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();

                var metricName = $"{eventSourceName}.{nameEnumerator.Current}";
                CollectionMetricsWithLabels(metricName, nameEnumerator.Current, payloadEnumerator.Current, currentLabels, longValues, doubleValues);
            }

            foreach (var longValue in longValues)
            {
                var currentMetric = LongMeasureMetrics?.GetOrAddEx(
                    longValue.Key,
                    (name) => _stats.Meter.CreateInt64Measure(name));
                currentMetric?.Record(default(SpanContext), longValue.Value, labels.ToList());
            }

            foreach (var doubleValue in doubleValues)
            {
                var currentMetric = DoubleMeasureMetrics?.GetOrAddEx(
                    doubleValue.Key,
                    (name) => _stats.Meter.CreateDoubleMeasure(name));
                currentMetric?.Record(default(SpanContext), doubleValue.Value, labels.ToList());
            }
        }

        private void CollectionMetricsWithLabels(
            string metricName,
            string payLoadName,
            object payloadValue,
            Dictionary<string, string> labels,
            Dictionary<string, long> longValues,
            Dictionary<string, double> doubleValues)
        {
            switch (payloadValue)
            {
                case string strValue:
                    labels.Add(payLoadName, strValue);
                    break;
                case short shortValue:
                    longValues.Add(metricName, shortValue);
                    break;
                case int intValue:
                    longValues.Add(metricName, intValue);
                    break;
                case uint unsignedInt:
                    longValues.Add(metricName, unsignedInt);
                    break;
                case long longValue:
                    longValues.Add(metricName, longValue);
                    break;
                case double doubleValue:
                    doubleValues.Add(metricName, doubleValue);
                    break;
                case bool boolValue:
                    longValues.Add(metricName, boolValue ? 1 : 0);
                    break;
                default:
                    Console.WriteLine($"Unhandled type at {metricName} - {payloadValue.GetType()} - {payloadValue}");
                    break;
            }

        }
    }
}