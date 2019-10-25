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

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixCommandOptions
    {
        IHystrixCommandGroupKey GroupKey { get; set; }

        IHystrixCommandKey CommandKey { get; set; }

        IHystrixThreadPoolKey ThreadPoolKey { get; set; }

        bool CircuitBreakerEnabled { get; set; }

        int CircuitBreakerErrorThresholdPercentage { get; set; }

        bool CircuitBreakerForceClosed { get; set; }

        bool CircuitBreakerForceOpen { get; set; }

        int CircuitBreakerRequestVolumeThreshold { get; set; }

        int CircuitBreakerSleepWindowInMilliseconds { get; set; }

        int ExecutionIsolationSemaphoreMaxConcurrentRequests { get; set; }

        ExecutionIsolationStrategy ExecutionIsolationStrategy { get; set; }

        string ExecutionIsolationThreadPoolKeyOverride { get; set; }

        int ExecutionTimeoutInMilliseconds { get; set; }

        bool ExecutionTimeoutEnabled { get; set; }

        int FallbackIsolationSemaphoreMaxConcurrentRequests { get; set; }

        bool FallbackEnabled { get; set; }

        int MetricsHealthSnapshotIntervalInMilliseconds { get; set; }

        int MetricsRollingPercentileBucketSize { get; set; }

        bool MetricsRollingPercentileEnabled { get; set; }

        int MetricsRollingPercentileWindow { get; set; }

        int MetricsRollingPercentileWindowInMilliseconds { get; set; }

        int MetricsRollingPercentileWindowBuckets { get; set; }

        int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

        int MetricsRollingStatisticalWindowBuckets { get; set; }

        bool RequestCacheEnabled { get; set; }

        bool RequestLogEnabled { get; set; }

        IHystrixThreadPoolOptions ThreadPoolOptions { get; set; }
    }
}
