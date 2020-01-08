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
using Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial
{
    public static class SerialHystrixUtilization
    {
        public static string ToJsonString(HystrixUtilization utilization)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    SerializeUtilization(writer, utilization);
                }

                return sw.ToString();
            }
        }

        private static void SerializeUtilization(JsonTextWriter json, HystrixUtilization utilization)
        {
            json.WriteStartObject();
            json.WriteStringField("type", "HystrixUtilization");
            json.WriteObjectFieldStart("commands");
            foreach (var entry in utilization.CommandUtilizationMap)
            {
                IHystrixCommandKey key = entry.Key;
                HystrixCommandUtilization commandUtilization = entry.Value;
                WriteCommandUtilizationJson(json, key, commandUtilization);
            }

            json.WriteEndObject();

            json.WriteObjectFieldStart("threadpools");
            foreach (var entry in utilization.ThreadPoolUtilizationMap)
            {
                IHystrixThreadPoolKey threadPoolKey = entry.Key;
                HystrixThreadPoolUtilization threadPoolUtilization = entry.Value;
                WriteThreadPoolUtilizationJson(json, threadPoolKey, threadPoolUtilization);
            }

            json.WriteEndObject();
            json.WriteEndObject();
        }

        private static void WriteCommandUtilizationJson(JsonTextWriter json, IHystrixCommandKey key, HystrixCommandUtilization utilization)
        {
            json.WriteObjectFieldStart(key.Name);
            json.WriteIntegerField("activeCount", utilization.ConcurrentCommandCount);
            json.WriteEndObject();
        }

        private static void WriteThreadPoolUtilizationJson(JsonTextWriter json, IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolUtilization utilization)
        {
            json.WriteObjectFieldStart(threadPoolKey.Name);
            json.WriteIntegerField("activeCount", utilization.CurrentActiveCount);
            json.WriteIntegerField("queueSize", utilization.CurrentQueueSize);
            json.WriteIntegerField("corePoolSize", utilization.CurrentCorePoolSize);
            json.WriteIntegerField("poolSize", utilization.CurrentPoolSize);
            json.WriteEndObject();
        }
    }
}
