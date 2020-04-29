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
using System.Globalization;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class EventSourceListener : EventListener
    {
        protected ConcurrentDictionary<string, MeasureMetric<long>> MeasureMetrics { get; set; }

        private readonly IStats _stats;

        internal EventSourceListener(IStats stats)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            MeasureMetrics = new ConcurrentDictionary<string, MeasureMetric<long>>();
        }

        protected Meter Meter => _stats.Meter;

        protected virtual void ExtractAndRecordMetric(string eventSourceName, EventWrittenEventArgs eventData, IDictionary<string, string> labels, string[] ignorePayloadNames = null)
        {
            var payloadNames = eventData.PayloadNames;
            var payload = eventData.Payload;

            using var nameEnumerator = payloadNames.Where(name => ignorePayloadNames == null || !ignorePayloadNames.Contains(name)).GetEnumerator();
            using var payloadEnumerator = payload.GetEnumerator();

            while (nameEnumerator.MoveNext())
            {
                payloadEnumerator.MoveNext();

                var metricName = $"{eventSourceName}.{nameEnumerator.Current}";
                var currentMetric = MeasureMetrics.GetOrAddEx(
                    metricName,
                    (name) => _stats.Meter.CreateInt64Measure(name));

                var actualValue = Convert.ToInt64(payloadEnumerator.Current, CultureInfo.InvariantCulture);
                currentMetric.Record(default(SpanContext), actualValue, labels.ToList());
            }
        }
    }
}