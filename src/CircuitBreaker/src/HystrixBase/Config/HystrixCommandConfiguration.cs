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

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixCommandConfiguration
    {
        private readonly IHystrixCommandKey commandKey;

        public HystrixCommandConfiguration(
            IHystrixCommandKey commandKey,
            IHystrixThreadPoolKey threadPoolKey,
            IHystrixCommandGroupKey groupKey,
            HystrixCommandExecutionConfig executionConfig,
            HystrixCommandCircuitBreakerConfig circuitBreakerConfig,
            HystrixCommandMetricsConfig metricsConfig)
        {
            this.commandKey = commandKey;
            ThreadPoolKey = threadPoolKey;
            GroupKey = groupKey;
            ExecutionConfig = executionConfig;
            CircuitBreakerConfig = circuitBreakerConfig;
            MetricsConfig = metricsConfig;
        }

        public static HystrixCommandConfiguration Sample(
            IHystrixCommandKey commandKey,
            IHystrixThreadPoolKey threadPoolKey,
            IHystrixCommandGroupKey groupKey,
            IHystrixCommandOptions commandProperties)
        {
            HystrixCommandExecutionConfig executionConfig = new HystrixCommandExecutionConfig(
                    commandProperties.ExecutionIsolationSemaphoreMaxConcurrentRequests,
                    commandProperties.ExecutionIsolationStrategy,
                    false,
                    commandProperties.ExecutionIsolationThreadPoolKeyOverride,
                    commandProperties.ExecutionTimeoutEnabled,
                    commandProperties.ExecutionTimeoutInMilliseconds,
                    commandProperties.FallbackEnabled,
                    commandProperties.FallbackIsolationSemaphoreMaxConcurrentRequests,
                    commandProperties.RequestCacheEnabled,
                    commandProperties.RequestLogEnabled);

            HystrixCommandCircuitBreakerConfig circuitBreakerConfig = new HystrixCommandCircuitBreakerConfig(
                    commandProperties.CircuitBreakerEnabled,
                    commandProperties.CircuitBreakerErrorThresholdPercentage,
                    commandProperties.CircuitBreakerForceClosed,
                    commandProperties.CircuitBreakerForceOpen,
                    commandProperties.CircuitBreakerRequestVolumeThreshold,
                    commandProperties.CircuitBreakerSleepWindowInMilliseconds);

            HystrixCommandMetricsConfig metricsConfig = new HystrixCommandMetricsConfig(
                    commandProperties.MetricsHealthSnapshotIntervalInMilliseconds,
                    commandProperties.MetricsRollingPercentileEnabled,
                    commandProperties.MetricsRollingPercentileWindowBuckets,
                    commandProperties.MetricsRollingPercentileWindowInMilliseconds,
                    commandProperties.MetricsRollingStatisticalWindowBuckets,
                    commandProperties.MetricsRollingStatisticalWindowInMilliseconds);

            return new HystrixCommandConfiguration(
                    commandKey, threadPoolKey, groupKey, executionConfig, circuitBreakerConfig, metricsConfig);
        }

        public IHystrixThreadPoolKey ThreadPoolKey { get; }

        public IHystrixCommandGroupKey GroupKey { get; }

        public HystrixCommandExecutionConfig ExecutionConfig { get; }

        public HystrixCommandCircuitBreakerConfig CircuitBreakerConfig { get; }

        public HystrixCommandMetricsConfig MetricsConfig { get; }

        public class HystrixCommandCircuitBreakerConfig
        {
            public HystrixCommandCircuitBreakerConfig(
                bool enabled,
                int errorThresholdPercentage,
                bool forceClosed,
                bool forceOpen,
                int requestVolumeThreshold,
                int sleepWindowInMilliseconds)
            {
                IsEnabled = enabled;
                ErrorThresholdPercentage = errorThresholdPercentage;
                IsForceClosed = forceClosed;
                IsForceOpen = forceOpen;
                RequestVolumeThreshold = requestVolumeThreshold;
                SleepWindowInMilliseconds = sleepWindowInMilliseconds;
            }

            public bool IsEnabled { get; }

            public int ErrorThresholdPercentage { get; }

            public bool IsForceClosed { get; }

            public bool IsForceOpen { get; }

            public int RequestVolumeThreshold { get; }

            public int SleepWindowInMilliseconds { get; }
        }

        public class HystrixCommandExecutionConfig
        {
            public HystrixCommandExecutionConfig(
                int semaphoreMaxConcurrentRequests,
                ExecutionIsolationStrategy isolationStrategy,
                bool threadInterruptOnTimeout,
                string threadPoolKeyOverride,
                bool timeoutEnabled,
                int timeoutInMilliseconds,
                bool fallbackEnabled,
                int fallbackMaxConcurrentRequests,
                bool requestCacheEnabled,
                bool requestLogEnabled)
            {
                SemaphoreMaxConcurrentRequests = semaphoreMaxConcurrentRequests;
                IsolationStrategy = isolationStrategy;
                IsThreadInterruptOnTimeout = threadInterruptOnTimeout;
                ThreadPoolKeyOverride = threadPoolKeyOverride;
                IsTimeoutEnabled = timeoutEnabled;
                TimeoutInMilliseconds = timeoutInMilliseconds;
                IsFallbackEnabled = fallbackEnabled;
                FallbackMaxConcurrentRequest = fallbackMaxConcurrentRequests;
                IsRequestCacheEnabled = requestCacheEnabled;
                IsRequestLogEnabled = requestLogEnabled;
            }

            public int SemaphoreMaxConcurrentRequests { get; }

            public ExecutionIsolationStrategy IsolationStrategy { get; }

            public bool IsThreadInterruptOnTimeout { get; }

            public string ThreadPoolKeyOverride { get; }

            public bool IsTimeoutEnabled { get; }

            public int TimeoutInMilliseconds { get; }

            public bool IsFallbackEnabled { get; }

            public int FallbackMaxConcurrentRequest { get; }

            public bool IsRequestCacheEnabled { get; }

            public bool IsRequestLogEnabled { get; }
        }

        public class HystrixCommandMetricsConfig
        {
            public HystrixCommandMetricsConfig(
                int healthIntervalInMilliseconds,
                bool rollingPercentileEnabled,
                int rollingPercentileNumberOfBuckets,
                int rollingPercentileBucketSizeInMilliseconds,
                int rollingCounterNumberOfBuckets,
                int rollingCounterBucketSizeInMilliseconds)
            {
                HealthIntervalInMilliseconds = healthIntervalInMilliseconds;
                IsRollingPercentileEnabled = rollingPercentileEnabled;
                RollingPercentileNumberOfBuckets = rollingPercentileNumberOfBuckets;
                RollingPercentileBucketSizeInMilliseconds = rollingPercentileBucketSizeInMilliseconds;
                RollingCounterNumberOfBuckets = rollingCounterNumberOfBuckets;
                RollingCounterBucketSizeInMilliseconds = rollingCounterBucketSizeInMilliseconds;
            }

            public int HealthIntervalInMilliseconds { get; }

            public bool IsRollingPercentileEnabled { get; }

            public int RollingPercentileNumberOfBuckets { get; }

            public int RollingPercentileBucketSizeInMilliseconds { get; }

            public int RollingCounterNumberOfBuckets { get; }

            public int RollingCounterBucketSizeInMilliseconds { get; }
        }
    }
}
