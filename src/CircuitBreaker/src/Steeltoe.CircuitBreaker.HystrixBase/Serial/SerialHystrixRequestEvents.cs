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

using Newtonsoft.Json;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial
{
    public static class SerialHystrixRequestEvents
    {
        public static string ToJsonString(HystrixRequestEvents requestEvents)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    SerializeRequestEvents(writer, requestEvents);
                }

                return sw.ToString();
            }
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
            ExecutionResult.EventCounts eventCounts = executionSignature.Eventcounts;
            foreach (HystrixEventType eventType in HystrixEventTypeHelper.Values)
            {
                if (!eventType.Equals(HystrixEventType.COLLAPSED))
                {
                    if (eventCounts.Contains(eventType))
                    {
                        int eventCount = eventCounts.GetCount(eventType);
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
            }

            json.WriteEndArray();
            json.WriteArrayFieldStart("latencies");
            foreach (int latency in latencies)
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
}
