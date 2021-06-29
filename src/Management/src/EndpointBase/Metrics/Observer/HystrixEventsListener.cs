// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
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
        private readonly Dictionary<string, string> _cktBreakerLabels = new Dictionary<string, string>();

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
                    ExtractAndRecordMetric(EventSourceName, eventData, _cktBreakerLabels);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.Message);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == EventSourceName)
            {
                SafelyEnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
            }
        }
    }
}