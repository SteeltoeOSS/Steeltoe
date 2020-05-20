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
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics.Observer
{
    public class HystrixEventsListener : EventSourceListener
    {
        private const string EventSourceName = "Steeltoe.Hystrix.Events";
        private static string[] _allowedEvents = new string[]
        {
            "CommandMetrics",
            "ThreadPoolMetrics",
            "CollapserMetrics",
        };

        private readonly ILogger<EventSourceListener> _logger;
        private readonly Dictionary<string, string> cktBreakerLabels = new Dictionary<string, string>();

        public HystrixEventsListener(IStats stats, ILogger<EventSourceListener> logger = null)
            : base(stats, logger)
        {
            _logger = logger;
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
                    ExtractAndRecordMetric(EventSourceName, eventData, cktBreakerLabels);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }
    }
}