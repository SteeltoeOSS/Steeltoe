// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class HystrixEventsListener : EventSourceListener
{
    private const string EventSourceName = "Steeltoe.Hystrix.Events";

    private static readonly string[] AllowedEvents =
    {
        "CommandMetrics",
        "ThreadPoolMetrics",
        "CollapserMetrics"
    };

    private readonly ILogger<EventSourceListener> _logger;
    private readonly Dictionary<string, object> _cktBreakerLabels = new();

    public HystrixEventsListener(ILogger<EventSourceListener> logger = null)
        : base(logger)
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
            if (AllowedEvents.Any(e => e.Equals(eventData.EventName, StringComparison.InvariantCulture)))
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
