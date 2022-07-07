// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial;

public static class SerialHystrixRequestEvents
{
    public static string ToJsonString(HystrixRequestEvents requestEvents)
    {
        using var sw = new StringWriter();
        using (var writer = new JsonTextWriter(sw))
        {
            SerializeRequestEvents(writer, requestEvents);
        }

        return sw.ToString();
    }

    private static void SerializeRequestEvents(JsonTextWriter json, HystrixRequestEvents requestEvents)
    {
        json.WriteStartArray();

        foreach (var entry in requestEvents.ExecutionsMappedToLatencies)
        {
            ConvertExecutionToJson(json, entry.Key, entry.Value);
        }

        json.WriteEndArray();
    }

    private static void ConvertExecutionToJson(JsonTextWriter json, ExecutionSignature executionSignature, List<int> latencies)
    {
        json.WriteStartObject();
        json.WriteStringField("name", executionSignature.CommandName);
        json.WriteArrayFieldStart("events");
        var eventCounts = executionSignature.Eventcounts;
        foreach (var eventType in HystrixEventTypeHelper.Values)
        {
            if (!eventType.Equals(HystrixEventType.COLLAPSED) && eventCounts.Contains(eventType))
            {
                var eventCount = eventCounts.GetCount(eventType);
                if (eventCount > 1)
                {
                    json.WriteStartObject();
                    json.WriteStringField("name", eventType.ToString());
                    json.WriteIntegerField("count", eventCount);
                    json.WriteEndObject();
                }
                else
                {
                    json.WriteValue(eventType.ToString());
                }
            }
        }

        json.WriteEndArray();
        json.WriteArrayFieldStart("latencies");
        foreach (var latency in latencies)
        {
            json.WriteValue(latency);
        }

        json.WriteEndArray();
        if (executionSignature.CachedCount > 0)
        {
            json.WriteIntegerField("cached", executionSignature.CachedCount);
        }

        if (executionSignature.Eventcounts.Contains(HystrixEventType.COLLAPSED))
        {
            json.WriteObjectFieldStart("collapsed");
            json.WriteStringField("name", executionSignature.CollapserKey.Name);
            json.WriteIntegerField("count", executionSignature.CollapserBatchSize);
            json.WriteEndObject();
        }

        json.WriteEndObject();
    }
}
