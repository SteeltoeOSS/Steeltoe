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
using Steeltoe.CircuitBreaker.Hystrix.Config;
using System.IO;

namespace Steeltoe.CircuitBreaker.Hystrix.Serial
{
    public static class SerialHystrixConfiguration
    {
        public static string ToJsonString(HystrixConfiguration config)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    SerializeConfiguration(writer, config);
                }

                return sw.ToString();
            }
        }

        private static void SerializeConfiguration(JsonTextWriter writer, HystrixConfiguration config)
        {
            writer.WriteStartObject();
            writer.WriteStringField("type", "HystrixConfig");
            writer.WriteObjectFieldStart("commands");
            foreach (var entry in config.CommandConfig)
            {
                IHystrixCommandKey key = entry.Key;
                HystrixCommandConfiguration commandConfig = entry.Value;
                WriteCommandConfigJson(writer, key, commandConfig);
            }

            writer.WriteEndObject();

            writer.WriteObjectFieldStart("threadpools");
            foreach (var entry in config.ThreadPoolConfig)
            {
                IHystrixThreadPoolKey threadPoolKey = entry.Key;
                HystrixThreadPoolConfiguration threadPoolConfig = entry.Value;
                WriteThreadPoolConfigJson(writer, threadPoolKey, threadPoolConfig);
            }

            writer.WriteEndObject();

            writer.WriteObjectFieldStart("collapsers");
            foreach (var entry in config.CollapserConfig)
            {
                IHystrixCollapserKey collapserKey = entry.Key;
                HystrixCollapserConfiguration collapserConfig = entry.Value;
                WriteCollapserConfigJson(writer, collapserKey, collapserConfig);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static void WriteCommandConfigJson(JsonTextWriter json, IHystrixCommandKey key, HystrixCommandConfiguration commandConfig)
        {
            json.WriteObjectFieldStart(key.Name);
            json.WriteStringField("threadPoolKey", commandConfig.ThreadPoolKey.Name);
            json.WriteStringField("groupKey", commandConfig.GroupKey.Name);
            json.WriteObjectFieldStart("execution");
            HystrixCommandConfiguration.HystrixCommandExecutionConfig executionConfig = commandConfig.ExecutionConfig;
            json.WriteStringField("isolationStrategy", executionConfig.IsolationStrategy.ToString());
            json.WriteStringField("threadPoolKeyOverride", executionConfig.ThreadPoolKeyOverride);
            json.WriteBooleanField("requestCacheEnabled", executionConfig.IsRequestCacheEnabled);
            json.WriteBooleanField("requestLogEnabled", executionConfig.IsRequestLogEnabled);
            json.WriteBooleanField("timeoutEnabled", executionConfig.IsTimeoutEnabled);
            json.WriteBooleanField("fallbackEnabled", executionConfig.IsFallbackEnabled);
            json.WriteIntegerField("timeoutInMilliseconds", executionConfig.TimeoutInMilliseconds);
            json.WriteIntegerField("semaphoreSize", executionConfig.SemaphoreMaxConcurrentRequests);
            json.WriteIntegerField("fallbackSemaphoreSize", executionConfig.FallbackMaxConcurrentRequest);
            json.WriteBooleanField("threadInterruptOnTimeout", executionConfig.IsThreadInterruptOnTimeout);
            json.WriteEndObject();
            json.WriteObjectFieldStart("metrics");
            HystrixCommandConfiguration.HystrixCommandMetricsConfig metricsConfig = commandConfig.MetricsConfig;
            json.WriteIntegerField("healthBucketSizeInMs", metricsConfig.HealthIntervalInMilliseconds);
            json.WriteIntegerField("percentileBucketSizeInMilliseconds", metricsConfig.RollingPercentileBucketSizeInMilliseconds);
            json.WriteIntegerField("percentileBucketCount", metricsConfig.RollingCounterNumberOfBuckets);
            json.WriteBooleanField("percentileEnabled", metricsConfig.IsRollingPercentileEnabled);
            json.WriteIntegerField("counterBucketSizeInMilliseconds", metricsConfig.RollingCounterBucketSizeInMilliseconds);
            json.WriteIntegerField("counterBucketCount", metricsConfig.RollingCounterNumberOfBuckets);
            json.WriteEndObject();
            json.WriteObjectFieldStart("circuitBreaker");
            HystrixCommandConfiguration.HystrixCommandCircuitBreakerConfig circuitBreakerConfig = commandConfig.CircuitBreakerConfig;
            json.WriteBooleanField("enabled", circuitBreakerConfig.IsEnabled);
            json.WriteBooleanField("isForcedOpen", circuitBreakerConfig.IsForceOpen);
            json.WriteBooleanField("isForcedClosed", circuitBreakerConfig.IsForceOpen);
            json.WriteIntegerField("requestVolumeThreshold", circuitBreakerConfig.RequestVolumeThreshold);
            json.WriteIntegerField("errorPercentageThreshold", circuitBreakerConfig.ErrorThresholdPercentage);
            json.WriteIntegerField("sleepInMilliseconds", circuitBreakerConfig.SleepWindowInMilliseconds);
            json.WriteEndObject();
            json.WriteEndObject();
        }

        private static void WriteThreadPoolConfigJson(JsonTextWriter json, IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolConfiguration threadPoolConfig)
        {
            json.WriteObjectFieldStart(threadPoolKey.Name);
            json.WriteIntegerField("coreSize", threadPoolConfig.CoreSize);
            json.WriteIntegerField("maximumSize", threadPoolConfig.MaximumSize);
            json.WriteIntegerField("maxQueueSize", threadPoolConfig.MaxQueueSize);
            json.WriteIntegerField("queueRejectionThreshold", threadPoolConfig.QueueRejectionThreshold);
            json.WriteIntegerField("keepAliveTimeInMinutes", threadPoolConfig.KeepAliveTimeInMinutes);
            json.WriteBooleanField("allowMaximumSizeToDivergeFromCoreSize", threadPoolConfig.AllowMaximumSizeToDivergeFromCoreSize);
            json.WriteIntegerField("counterBucketSizeInMilliseconds", threadPoolConfig.RollingCounterBucketSizeInMilliseconds);
            json.WriteIntegerField("counterBucketCount", threadPoolConfig.RollingCounterNumberOfBuckets);
            json.WriteEndObject();
        }

        private static void WriteCollapserConfigJson(JsonTextWriter json, IHystrixCollapserKey collapserKey, HystrixCollapserConfiguration collapserConfig)
        {
            json.WriteObjectFieldStart(collapserKey.Name);
            json.WriteIntegerField("maxRequestsInBatch", collapserConfig.MaxRequestsInBatch);
            json.WriteIntegerField("timerDelayInMilliseconds", collapserConfig.TimerDelayInMilliseconds);
            json.WriteBooleanField("requestCacheEnabled", collapserConfig.IsRequestCacheEnabled);
            json.WriteObjectFieldStart("metrics");
            HystrixCollapserConfiguration.CollapserMetricsConfig metricsConfig = collapserConfig.CollapserMetricsConfiguration;
            json.WriteIntegerField("percentileBucketSizeInMilliseconds", metricsConfig.RollingPercentileBucketSizeInMilliseconds);
            json.WriteIntegerField("percentileBucketCount", metricsConfig.RollingCounterNumberOfBuckets);
            json.WriteBooleanField("percentileEnabled", metricsConfig.IsRollingPercentileEnabled);
            json.WriteIntegerField("counterBucketSizeInMilliseconds", metricsConfig.RollingCounterBucketSizeInMilliseconds);
            json.WriteIntegerField("counterBucketCount", metricsConfig.RollingCounterNumberOfBuckets);
            json.WriteEndObject();
            json.WriteEndObject();
        }
    }
}
