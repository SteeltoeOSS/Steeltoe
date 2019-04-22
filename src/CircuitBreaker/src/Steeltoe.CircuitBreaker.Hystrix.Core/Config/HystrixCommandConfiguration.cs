//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;


namespace Steeltoe.CircuitBreaker.Hystrix.Config
{
    public class HystrixCommandConfiguration
    {

        private readonly IHystrixCommandKey commandKey;
        private readonly IHystrixThreadPoolKey threadPoolKey;
        private readonly IHystrixCommandGroupKey groupKey;
        private readonly HystrixCommandExecutionConfig executionConfig;
        private readonly HystrixCommandCircuitBreakerConfig circuitBreakerConfig;
        private readonly HystrixCommandMetricsConfig metricsConfig;

        public HystrixCommandConfiguration(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, IHystrixCommandGroupKey groupKey,
                                           HystrixCommandExecutionConfig executionConfig,
                                           HystrixCommandCircuitBreakerConfig circuitBreakerConfig,
                                           HystrixCommandMetricsConfig metricsConfig)
        {
            this.commandKey = commandKey;
            this.threadPoolKey = threadPoolKey;
            this.groupKey = groupKey;
            this.executionConfig = executionConfig;
            this.circuitBreakerConfig = circuitBreakerConfig;
            this.metricsConfig = metricsConfig;
        }

        public static HystrixCommandConfiguration Sample(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey,
                                                         IHystrixCommandGroupKey groupKey, IHystrixCommandOptions commandProperties)
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
                    commandProperties.RequestLogEnabled
            );

            HystrixCommandCircuitBreakerConfig circuitBreakerConfig = new HystrixCommandCircuitBreakerConfig(
                    commandProperties.CircuitBreakerEnabled,
                    commandProperties.CircuitBreakerErrorThresholdPercentage,
                    commandProperties.CircuitBreakerForceClosed,
                    commandProperties.CircuitBreakerForceOpen,
                    commandProperties.CircuitBreakerRequestVolumeThreshold,
                    commandProperties.CircuitBreakerSleepWindowInMilliseconds
            );

            HystrixCommandMetricsConfig metricsConfig = new HystrixCommandMetricsConfig(
                    commandProperties.MetricsHealthSnapshotIntervalInMilliseconds,
                    commandProperties.MetricsRollingPercentileEnabled,
                    commandProperties.MetricsRollingPercentileWindowBuckets,
                    commandProperties.MetricsRollingPercentileWindowInMilliseconds,
                    commandProperties.MetricsRollingStatisticalWindowBuckets,
                    commandProperties.MetricsRollingStatisticalWindowInMilliseconds
            );

            return new HystrixCommandConfiguration(
                    commandKey, threadPoolKey, groupKey, executionConfig, circuitBreakerConfig, metricsConfig);
        }

        public IHystrixThreadPoolKey ThreadPoolKey
        {
            get { return threadPoolKey; }
        }

        public IHystrixCommandGroupKey GroupKey
        {
            get { return groupKey; }
        }

        public HystrixCommandExecutionConfig ExecutionConfig
        {
            get { return executionConfig; }
        }

        public HystrixCommandCircuitBreakerConfig CircuitBreakerConfig
        {
            get { return circuitBreakerConfig; }
        }

        public HystrixCommandMetricsConfig MetricsConfig
        {
            get { return metricsConfig; }
        }

        public class HystrixCommandCircuitBreakerConfig
        {
            private readonly bool enabled;
            private readonly int errorThresholdPercentage;
            private readonly bool forceClosed;
            private readonly bool forceOpen;
            private readonly int requestVolumeThreshold;
            private readonly int sleepWindowInMilliseconds;

            public HystrixCommandCircuitBreakerConfig(bool enabled, int errorThresholdPercentage, bool forceClosed,
                                                      bool forceOpen, int requestVolumeThreshold, int sleepWindowInMilliseconds)
            {
                this.enabled = enabled;
                this.errorThresholdPercentage = errorThresholdPercentage;
                this.forceClosed = forceClosed;
                this.forceOpen = forceOpen;
                this.requestVolumeThreshold = requestVolumeThreshold;
                this.sleepWindowInMilliseconds = sleepWindowInMilliseconds;
            }

            public bool IsEnabled
            {
                get { return enabled; }
            }

            public int ErrorThresholdPercentage
            {
                get { return errorThresholdPercentage; }
            }

            public bool IsForceClosed
            {
                get { return forceClosed; }
            }

            public bool IsForceOpen
            {
                get { return forceOpen; }
            }

            public int RequestVolumeThreshold
            {
                get { return requestVolumeThreshold; }
            }

            public int SleepWindowInMilliseconds
            {
                get { return sleepWindowInMilliseconds; }
            }
        }

        public class HystrixCommandExecutionConfig
        {
            private readonly int semaphoreMaxConcurrentRequests;
            private readonly ExecutionIsolationStrategy isolationStrategy;
            private readonly bool threadInterruptOnTimeout;
            private readonly String threadPoolKeyOverride;
            private readonly bool timeoutEnabled;
            private readonly int timeoutInMilliseconds;
            private readonly bool fallbackEnabled;
            private readonly int fallbackMaxConcurrentRequest;
            private readonly bool requestCacheEnabled;
            private readonly bool requestLogEnabled;

            public HystrixCommandExecutionConfig(int semaphoreMaxConcurrentRequests, ExecutionIsolationStrategy isolationStrategy,
                                                 bool threadInterruptOnTimeout, String threadPoolKeyOverride, bool timeoutEnabled,
                                                 int timeoutInMilliseconds, bool fallbackEnabled, int fallbackMaxConcurrentRequests,
                                                 bool requestCacheEnabled, bool requestLogEnabled)
            {
                this.semaphoreMaxConcurrentRequests = semaphoreMaxConcurrentRequests;
                this.isolationStrategy = isolationStrategy;
                this.threadInterruptOnTimeout = threadInterruptOnTimeout;
                this.threadPoolKeyOverride = threadPoolKeyOverride;
                this.timeoutEnabled = timeoutEnabled;
                this.timeoutInMilliseconds = timeoutInMilliseconds;
                this.fallbackEnabled = fallbackEnabled;
                this.fallbackMaxConcurrentRequest = fallbackMaxConcurrentRequests;
                this.requestCacheEnabled = requestCacheEnabled;
                this.requestLogEnabled = requestLogEnabled;

            }

            public int SemaphoreMaxConcurrentRequests
            {
                get { return semaphoreMaxConcurrentRequests; }
            }

            public ExecutionIsolationStrategy IsolationStrategy
            {
                get { return isolationStrategy; }
            }

            public bool IsThreadInterruptOnTimeout
            {
                get { return threadInterruptOnTimeout; }
            }

            public String ThreadPoolKeyOverride
            {
                get { return threadPoolKeyOverride; }
            }

            public bool IsTimeoutEnabled
            {
                get { return timeoutEnabled; }
            }

            public int TimeoutInMilliseconds
            {
                get { return timeoutInMilliseconds; }
            }

            public bool IsFallbackEnabled
            {
                get { return fallbackEnabled; }
            }

            public int FallbackMaxConcurrentRequest
            {
                get { return fallbackMaxConcurrentRequest; }
            }

            public bool IsRequestCacheEnabled
            {
                get { return requestCacheEnabled; }
            }

            public bool IsRequestLogEnabled
            {
                get { return requestLogEnabled; }
            }
        }

        public class HystrixCommandMetricsConfig
        {
            private readonly int healthIntervalInMilliseconds;
            private readonly bool rollingPercentileEnabled;
            private readonly int rollingPercentileNumberOfBuckets;
            private readonly int rollingPercentileBucketSizeInMilliseconds;
            private readonly int rollingCounterNumberOfBuckets;
            private readonly int rollingCounterBucketSizeInMilliseconds;

            public HystrixCommandMetricsConfig(int healthIntervalInMilliseconds, bool rollingPercentileEnabled, int rollingPercentileNumberOfBuckets,
                                               int rollingPercentileBucketSizeInMilliseconds, int rollingCounterNumberOfBuckets,
                                               int rollingCounterBucketSizeInMilliseconds)
            {
                this.healthIntervalInMilliseconds = healthIntervalInMilliseconds;
                this.rollingPercentileEnabled = rollingPercentileEnabled;
                this.rollingPercentileNumberOfBuckets = rollingPercentileNumberOfBuckets;
                this.rollingPercentileBucketSizeInMilliseconds = rollingPercentileBucketSizeInMilliseconds;
                this.rollingCounterNumberOfBuckets = rollingCounterNumberOfBuckets;
                this.rollingCounterBucketSizeInMilliseconds = rollingCounterBucketSizeInMilliseconds;
            }

            public int HealthIntervalInMilliseconds
            {
                get { return healthIntervalInMilliseconds; }
            }

            public bool IsRollingPercentileEnabled
            {
                get { return rollingPercentileEnabled; }
            }

            public int RollingPercentileNumberOfBuckets
            {
                get { return rollingPercentileNumberOfBuckets; }
            }

            public int RollingPercentileBucketSizeInMilliseconds
            {
                get { return rollingPercentileBucketSizeInMilliseconds; }
            }

            public int RollingCounterNumberOfBuckets
            {
                get { return rollingCounterNumberOfBuckets; }
            }

            public int RollingCounterBucketSizeInMilliseconds
            {
                get { return rollingCounterBucketSizeInMilliseconds; }
            }
        }
    }

}
