// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCommandOptions : HystrixBaseOptions, IHystrixCommandOptions
{
    internal const int Default_MetricsRollingStatisticalWindow = 10000; // default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)
    internal const int Default_MetricsRollingStatisticalWindowBuckets = 10; // default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second
    internal const int Default_CircuitBreakerRequestVolumeThreshold = 20; // default => statisticalWindowVolumeThreshold: 20 requests in 10 seconds must occur before statistics matter
    internal const int Default_CircuitBreakerSleepWindowInMilliseconds = 5000; // default => sleepWindow: 5000 = 5 seconds that we will sleep before trying again after tripping the circuit
    internal const int Default_CircuitBreakerErrorThresholdPercentage = 50; // default => errorThresholdPercentage = 50 = if 50%+ of requests in 10 seconds are failures or latent then we will trip the circuit
    internal const bool Default_CircuitBreakerForceOpen = false; // default => forceCircuitOpen = false (we want to allow traffic)
    internal const bool Default_CircuitBreakerForceClosed = false; // default => ignoreErrors = false
    internal const int Default_ExecutionTimeoutInMilliseconds = 1000; // default => executionTimeoutInMilliseconds: 1000 = 1 second
    internal const bool Default_ExecutionTimeoutEnabled = true;
    internal const ExecutionIsolationStrategy Default_IsolationStrategy = ExecutionIsolationStrategy.THREAD;
    internal const bool Default_MetricsRollingPercentileEnabled = true;
    internal const bool Default_RequestCacheEnabled = true;
    internal const int Default_FallbackIsolationSemaphoreMaxConcurrentRequests = 10;
    internal const bool Default_FallbackEnabled = true;
    internal const int Default_ExecutionIsolationSemaphoreMaxConcurrentRequests = 10;
    internal const bool Default_RequestLogEnabled = true;
    internal const bool Default_CircuitBreakerEnabled = true;
    internal const int Default_MetricsRollingPercentileWindow = 60000; // default to 1 minute for RollingPercentile
    internal const int Default_MetricsRollingPercentileWindowBuckets = 6; // default to 6 buckets (10 seconds each in 60 second window)
    internal const int Default_MetricsRollingPercentileBucketSize = 100; // default to 100 values max per bucket
    internal const int Default_MetricsHealthSnapshotIntervalInMilliseconds = 500; // default to 500ms as max frequency between allowing snapshots of health (error percentage etc)

    protected const string HYSTRIX_COMMAND_PREFIX = "hystrix:command";

    protected IHystrixCommandOptions defaults = null;

    public HystrixCommandOptions(
        IHystrixCommandGroupKey groupKey,
        IHystrixCommandKey key,
        IHystrixCommandOptions defaults = null,
        IHystrixDynamicOptions dynamic = null)
        : this(key, defaults, dynamic)
    {
        GroupKey = groupKey;
    }

    public HystrixCommandOptions(
        IHystrixCommandKey key,
        IHystrixCommandOptions defaults = null,
        IHystrixDynamicOptions dynamic = null)
        : this(defaults, dynamic)
    {
        CommandKey = key;
        CircuitBreakerEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:enabled", Default_CircuitBreakerEnabled, defaults?.CircuitBreakerEnabled);
        CircuitBreakerRequestVolumeThreshold = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:requestVolumeThreshold", Default_CircuitBreakerRequestVolumeThreshold, defaults?.CircuitBreakerRequestVolumeThreshold);
        CircuitBreakerSleepWindowInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:sleepWindowInMilliseconds", Default_CircuitBreakerSleepWindowInMilliseconds, defaults?.CircuitBreakerSleepWindowInMilliseconds);
        CircuitBreakerErrorThresholdPercentage = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:errorThresholdPercentage", Default_CircuitBreakerErrorThresholdPercentage, defaults?.CircuitBreakerErrorThresholdPercentage);
        CircuitBreakerForceOpen = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:forceOpen", Default_CircuitBreakerForceOpen, defaults?.CircuitBreakerForceOpen);
        CircuitBreakerForceClosed = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "circuitBreaker:forceClosed", Default_CircuitBreakerForceClosed, defaults?.CircuitBreakerForceClosed);
        ExecutionIsolationStrategy = GetIsolationStrategy(key);
        ExecutionTimeoutInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "execution:isolation:thread:timeoutInMilliseconds", Default_ExecutionTimeoutInMilliseconds, defaults?.ExecutionTimeoutInMilliseconds);
        ExecutionTimeoutEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "execution:timeout:enabled", Default_ExecutionTimeoutEnabled, defaults?.ExecutionTimeoutEnabled);
        ExecutionIsolationSemaphoreMaxConcurrentRequests = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "execution:isolation:semaphore:maxConcurrentRequests", Default_ExecutionIsolationSemaphoreMaxConcurrentRequests, defaults?.ExecutionIsolationSemaphoreMaxConcurrentRequests);
        FallbackIsolationSemaphoreMaxConcurrentRequests = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "fallback:isolation:semaphore:maxConcurrentRequests", Default_FallbackIsolationSemaphoreMaxConcurrentRequests, defaults?.FallbackIsolationSemaphoreMaxConcurrentRequests);
        FallbackEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "fallback:enabled", Default_FallbackEnabled, defaults?.FallbackEnabled);

        MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingStats:timeInMilliseconds", Default_MetricsRollingStatisticalWindow, defaults?.MetricsRollingStatisticalWindowInMilliseconds);
        MetricsRollingStatisticalWindowBuckets = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingStats:numBuckets", Default_MetricsRollingStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets);
        MetricsRollingPercentileEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:enabled", Default_MetricsRollingPercentileEnabled, defaults?.MetricsRollingPercentileEnabled);
        MetricsRollingPercentileWindowInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:timeInMilliseconds", Default_MetricsRollingPercentileWindow, defaults?.MetricsRollingPercentileWindowInMilliseconds);
        MetricsRollingPercentileWindowBuckets = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:numBuckets", Default_MetricsRollingPercentileWindowBuckets, defaults?.MetricsRollingPercentileWindowBuckets);
        MetricsRollingPercentileBucketSize = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:rollingPercentile:bucketSize", Default_MetricsRollingPercentileBucketSize, defaults?.MetricsRollingPercentileBucketSize);
        MetricsHealthSnapshotIntervalInMilliseconds = GetInteger(HYSTRIX_COMMAND_PREFIX, key.Name, "metrics:healthSnapshot:intervalInMilliseconds", Default_MetricsHealthSnapshotIntervalInMilliseconds, defaults?.MetricsHealthSnapshotIntervalInMilliseconds);

        RequestCacheEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "requestCache:enabled", Default_RequestCacheEnabled, defaults?.RequestCacheEnabled);
        RequestLogEnabled = GetBoolean(HYSTRIX_COMMAND_PREFIX, key.Name, "requestLog:enabled", Default_RequestLogEnabled, defaults?.RequestLogEnabled);

        ExecutionIsolationThreadPoolKeyOverride = GetThreadPoolKeyOverride(HYSTRIX_COMMAND_PREFIX, key.Name, "threadPoolKeyOverride", null, defaults?.ExecutionIsolationThreadPoolKeyOverride);
    }

    internal HystrixCommandOptions(IHystrixCommandOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : base(dynamic)
    {
        this.defaults = defaults;
        CommandKey = null;
        CircuitBreakerEnabled = Default_CircuitBreakerEnabled;
        CircuitBreakerRequestVolumeThreshold = Default_CircuitBreakerRequestVolumeThreshold;
        CircuitBreakerSleepWindowInMilliseconds = Default_CircuitBreakerSleepWindowInMilliseconds;
        CircuitBreakerErrorThresholdPercentage = Default_CircuitBreakerErrorThresholdPercentage;
        CircuitBreakerForceOpen = Default_CircuitBreakerForceOpen;
        CircuitBreakerForceClosed = Default_CircuitBreakerForceClosed;
        ExecutionIsolationStrategy = Default_IsolationStrategy;

        ExecutionTimeoutInMilliseconds = Default_ExecutionTimeoutInMilliseconds;
        ExecutionTimeoutEnabled = Default_ExecutionTimeoutEnabled;
        ExecutionIsolationSemaphoreMaxConcurrentRequests = Default_ExecutionIsolationSemaphoreMaxConcurrentRequests;
        FallbackIsolationSemaphoreMaxConcurrentRequests = Default_FallbackIsolationSemaphoreMaxConcurrentRequests;
        FallbackEnabled = Default_FallbackEnabled;
        MetricsRollingStatisticalWindowInMilliseconds = Default_MetricsRollingStatisticalWindow;
        MetricsRollingStatisticalWindowBuckets = Default_MetricsRollingStatisticalWindowBuckets;
        MetricsRollingPercentileEnabled = Default_MetricsRollingPercentileEnabled;
        MetricsRollingPercentileWindowInMilliseconds = Default_MetricsRollingPercentileWindow;
        MetricsRollingPercentileWindowBuckets = Default_MetricsRollingPercentileWindowBuckets;
        MetricsRollingPercentileBucketSize = Default_MetricsRollingPercentileBucketSize;
        MetricsHealthSnapshotIntervalInMilliseconds = Default_MetricsHealthSnapshotIntervalInMilliseconds;
        RequestCacheEnabled = Default_RequestCacheEnabled;
        RequestLogEnabled = Default_RequestLogEnabled;
    }

    public IHystrixCommandGroupKey GroupKey { get; set; }

    public IHystrixCommandKey CommandKey { get; set; }

    public IHystrixThreadPoolKey ThreadPoolKey { get; set; }

    public virtual bool CircuitBreakerEnabled { get; set; }

    public virtual int CircuitBreakerErrorThresholdPercentage { get; set; }

    public virtual bool CircuitBreakerForceClosed { get; set; }

    public virtual bool CircuitBreakerForceOpen { get; set; }

    public virtual int CircuitBreakerRequestVolumeThreshold { get; set; }

    public virtual int CircuitBreakerSleepWindowInMilliseconds { get; set; }

    public virtual int ExecutionIsolationSemaphoreMaxConcurrentRequests { get; set; }

    public virtual ExecutionIsolationStrategy ExecutionIsolationStrategy { get; set; }

    public virtual string ExecutionIsolationThreadPoolKeyOverride { get; set; }

    public virtual int ExecutionTimeoutInMilliseconds { get; set; }

    public virtual bool ExecutionTimeoutEnabled { get; set; }

    public virtual int FallbackIsolationSemaphoreMaxConcurrentRequests { get; set; }

    public virtual bool FallbackEnabled { get; set; }

    public virtual int MetricsHealthSnapshotIntervalInMilliseconds { get; set; }

    public virtual int MetricsRollingPercentileBucketSize { get; set; }

    public virtual bool MetricsRollingPercentileEnabled { get; set; }

    public virtual int MetricsRollingPercentileWindow { get; set; }

    public virtual int MetricsRollingPercentileWindowInMilliseconds { get; set; }

    public virtual int MetricsRollingPercentileWindowBuckets { get; set; }

    public virtual int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

    public virtual int MetricsRollingStatisticalWindowBuckets { get; set; }

    public virtual bool RequestCacheEnabled { get; set; }

    public virtual bool RequestLogEnabled { get; set; }

    public virtual IHystrixThreadPoolOptions ThreadPoolOptions { get; set; }

    private ExecutionIsolationStrategy GetIsolationStrategy(IHystrixCommandKey key)
    {
        var isolation = GetString(HYSTRIX_COMMAND_PREFIX, key.Name, "execution.isolation.strategy", Default_IsolationStrategy.ToString(), defaults?.ExecutionIsolationStrategy.ToString());
        if (ExecutionIsolationStrategy.THREAD.ToString().Equals(isolation, StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionIsolationStrategy.THREAD;
        }

        if (ExecutionIsolationStrategy.SEMAPHORE.ToString().Equals(isolation, StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionIsolationStrategy.SEMAPHORE;
        }

        throw new ArgumentOutOfRangeException("execution.isolation.strategy");
    }

    private string GetThreadPoolKeyOverride(string prefix, string key, string property, string globalDefault, string instanceDefaultFromCode)
    {
        var result = globalDefault;
        result = instanceDefaultFromCode ?? result; // instance default from code
        result = (_dynamic != null) ? _dynamic.GetString(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
        return result;
    }
}