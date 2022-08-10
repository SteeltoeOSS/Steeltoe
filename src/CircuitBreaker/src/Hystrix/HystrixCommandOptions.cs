// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCommandOptions : HystrixBaseOptions, IHystrixCommandOptions
{
    internal const int
        DefaultMetricsRollingStatisticalWindow =
            10000; // default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)

    internal const int
        DefaultMetricsRollingStatisticalWindowBuckets =
            10; // default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second

    internal const int
        DefaultCircuitBreakerRequestVolumeThreshold =
            20; // default => statisticalWindowVolumeThreshold: 20 requests in 10 seconds must occur before statistics matter

    internal const int
        DefaultCircuitBreakerSleepWindowInMilliseconds =
            5000; // default => sleepWindow: 5000 = 5 seconds that we will sleep before trying again after tripping the circuit

    internal const int
        DefaultCircuitBreakerErrorThresholdPercentage =
            50; // default => errorThresholdPercentage = 50 = if 50%+ of requests in 10 seconds are failures or latent then we will trip the circuit

    internal const bool DefaultCircuitBreakerForceOpen = false; // default => forceCircuitOpen = false (we want to allow traffic)
    internal const bool DefaultCircuitBreakerForceClosed = false; // default => ignoreErrors = false
    internal const int DefaultExecutionTimeoutInMilliseconds = 1000; // default => executionTimeoutInMilliseconds: 1000 = 1 second
    internal const bool DefaultExecutionTimeoutEnabled = true;
    internal const ExecutionIsolationStrategy DefaultIsolationStrategy = ExecutionIsolationStrategy.Thread;
    internal const bool DefaultMetricsRollingPercentileEnabled = true;
    internal const bool DefaultRequestCacheEnabled = true;
    internal const int DefaultFallbackIsolationSemaphoreMaxConcurrentRequests = 10;
    internal const bool DefaultFallbackEnabled = true;
    internal const int DefaultExecutionIsolationSemaphoreMaxConcurrentRequests = 10;
    internal const bool DefaultRequestLogEnabled = true;
    internal const bool DefaultCircuitBreakerEnabled = true;
    internal const int DefaultMetricsRollingPercentileWindow = 60000; // default to 1 minute for RollingPercentile
    internal const int DefaultMetricsRollingPercentileWindowBuckets = 6; // default to 6 buckets (10 seconds each in 60 second window)
    internal const int DefaultMetricsRollingPercentileBucketSize = 100; // default to 100 values max per bucket

    internal const int
        DefaultMetricsHealthSnapshotIntervalInMilliseconds =
            500; // default to 500ms as max frequency between allowing snapshots of health (error percentage etc)

    protected const string HystrixCommandPrefix = "hystrix:command";

    protected IHystrixCommandOptions defaults;

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

    public HystrixCommandOptions(IHystrixCommandGroupKey groupKey, IHystrixCommandKey key, IHystrixCommandOptions defaults = null,
        IHystrixDynamicOptions dynamic = null)
        : this(key, defaults, dynamic)
    {
        GroupKey = groupKey;
    }

    public HystrixCommandOptions(IHystrixCommandKey key, IHystrixCommandOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : this(defaults, dynamic)
    {
        CommandKey = key;

        CircuitBreakerEnabled = GetBoolean(HystrixCommandPrefix, key.Name, "circuitBreaker:enabled", DefaultCircuitBreakerEnabled,
            defaults?.CircuitBreakerEnabled);

        CircuitBreakerRequestVolumeThreshold = GetInteger(HystrixCommandPrefix, key.Name, "circuitBreaker:requestVolumeThreshold",
            DefaultCircuitBreakerRequestVolumeThreshold, defaults?.CircuitBreakerRequestVolumeThreshold);

        CircuitBreakerSleepWindowInMilliseconds = GetInteger(HystrixCommandPrefix, key.Name, "circuitBreaker:sleepWindowInMilliseconds",
            DefaultCircuitBreakerSleepWindowInMilliseconds, defaults?.CircuitBreakerSleepWindowInMilliseconds);

        CircuitBreakerErrorThresholdPercentage = GetInteger(HystrixCommandPrefix, key.Name, "circuitBreaker:errorThresholdPercentage",
            DefaultCircuitBreakerErrorThresholdPercentage, defaults?.CircuitBreakerErrorThresholdPercentage);

        CircuitBreakerForceOpen = GetBoolean(HystrixCommandPrefix, key.Name, "circuitBreaker:forceOpen", DefaultCircuitBreakerForceOpen,
            defaults?.CircuitBreakerForceOpen);

        CircuitBreakerForceClosed = GetBoolean(HystrixCommandPrefix, key.Name, "circuitBreaker:forceClosed", DefaultCircuitBreakerForceClosed,
            defaults?.CircuitBreakerForceClosed);

        ExecutionIsolationStrategy = GetIsolationStrategy(key);

        ExecutionTimeoutInMilliseconds = GetInteger(HystrixCommandPrefix, key.Name, "execution:isolation:thread:timeoutInMilliseconds",
            DefaultExecutionTimeoutInMilliseconds, defaults?.ExecutionTimeoutInMilliseconds);

        ExecutionTimeoutEnabled = GetBoolean(HystrixCommandPrefix, key.Name, "execution:timeout:enabled", DefaultExecutionTimeoutEnabled,
            defaults?.ExecutionTimeoutEnabled);

        ExecutionIsolationSemaphoreMaxConcurrentRequests = GetInteger(HystrixCommandPrefix, key.Name, "execution:isolation:semaphore:maxConcurrentRequests",
            DefaultExecutionIsolationSemaphoreMaxConcurrentRequests, defaults?.ExecutionIsolationSemaphoreMaxConcurrentRequests);

        FallbackIsolationSemaphoreMaxConcurrentRequests = GetInteger(HystrixCommandPrefix, key.Name, "fallback:isolation:semaphore:maxConcurrentRequests",
            DefaultFallbackIsolationSemaphoreMaxConcurrentRequests, defaults?.FallbackIsolationSemaphoreMaxConcurrentRequests);

        FallbackEnabled = GetBoolean(HystrixCommandPrefix, key.Name, "fallback:enabled", DefaultFallbackEnabled, defaults?.FallbackEnabled);

        MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HystrixCommandPrefix, key.Name, "metrics:rollingStats:timeInMilliseconds",
            DefaultMetricsRollingStatisticalWindow, defaults?.MetricsRollingStatisticalWindowInMilliseconds);

        MetricsRollingStatisticalWindowBuckets = GetInteger(HystrixCommandPrefix, key.Name, "metrics:rollingStats:numBuckets",
            DefaultMetricsRollingStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets);

        MetricsRollingPercentileEnabled = GetBoolean(HystrixCommandPrefix, key.Name, "metrics:rollingPercentile:enabled",
            DefaultMetricsRollingPercentileEnabled, defaults?.MetricsRollingPercentileEnabled);

        MetricsRollingPercentileWindowInMilliseconds = GetInteger(HystrixCommandPrefix, key.Name, "metrics:rollingPercentile:timeInMilliseconds",
            DefaultMetricsRollingPercentileWindow, defaults?.MetricsRollingPercentileWindowInMilliseconds);

        MetricsRollingPercentileWindowBuckets = GetInteger(HystrixCommandPrefix, key.Name, "metrics:rollingPercentile:numBuckets",
            DefaultMetricsRollingPercentileWindowBuckets, defaults?.MetricsRollingPercentileWindowBuckets);

        MetricsRollingPercentileBucketSize = GetInteger(HystrixCommandPrefix, key.Name, "metrics:rollingPercentile:bucketSize",
            DefaultMetricsRollingPercentileBucketSize, defaults?.MetricsRollingPercentileBucketSize);

        MetricsHealthSnapshotIntervalInMilliseconds = GetInteger(HystrixCommandPrefix, key.Name, "metrics:healthSnapshot:intervalInMilliseconds",
            DefaultMetricsHealthSnapshotIntervalInMilliseconds, defaults?.MetricsHealthSnapshotIntervalInMilliseconds);

        RequestCacheEnabled = GetBoolean(HystrixCommandPrefix, key.Name, "requestCache:enabled", DefaultRequestCacheEnabled, defaults?.RequestCacheEnabled);
        RequestLogEnabled = GetBoolean(HystrixCommandPrefix, key.Name, "requestLog:enabled", DefaultRequestLogEnabled, defaults?.RequestLogEnabled);

        ExecutionIsolationThreadPoolKeyOverride = GetThreadPoolKeyOverride(HystrixCommandPrefix, key.Name, "threadPoolKeyOverride", null,
            defaults?.ExecutionIsolationThreadPoolKeyOverride);
    }

    internal HystrixCommandOptions(IHystrixCommandOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : base(dynamic)
    {
        this.defaults = defaults;
        CommandKey = null;
        CircuitBreakerEnabled = DefaultCircuitBreakerEnabled;
        CircuitBreakerRequestVolumeThreshold = DefaultCircuitBreakerRequestVolumeThreshold;
        CircuitBreakerSleepWindowInMilliseconds = DefaultCircuitBreakerSleepWindowInMilliseconds;
        CircuitBreakerErrorThresholdPercentage = DefaultCircuitBreakerErrorThresholdPercentage;
        CircuitBreakerForceOpen = DefaultCircuitBreakerForceOpen;
        CircuitBreakerForceClosed = DefaultCircuitBreakerForceClosed;
        ExecutionIsolationStrategy = DefaultIsolationStrategy;

        ExecutionTimeoutInMilliseconds = DefaultExecutionTimeoutInMilliseconds;
        ExecutionTimeoutEnabled = DefaultExecutionTimeoutEnabled;
        ExecutionIsolationSemaphoreMaxConcurrentRequests = DefaultExecutionIsolationSemaphoreMaxConcurrentRequests;
        FallbackIsolationSemaphoreMaxConcurrentRequests = DefaultFallbackIsolationSemaphoreMaxConcurrentRequests;
        FallbackEnabled = DefaultFallbackEnabled;
        MetricsRollingStatisticalWindowInMilliseconds = DefaultMetricsRollingStatisticalWindow;
        MetricsRollingStatisticalWindowBuckets = DefaultMetricsRollingStatisticalWindowBuckets;
        MetricsRollingPercentileEnabled = DefaultMetricsRollingPercentileEnabled;
        MetricsRollingPercentileWindowInMilliseconds = DefaultMetricsRollingPercentileWindow;
        MetricsRollingPercentileWindowBuckets = DefaultMetricsRollingPercentileWindowBuckets;
        MetricsRollingPercentileBucketSize = DefaultMetricsRollingPercentileBucketSize;
        MetricsHealthSnapshotIntervalInMilliseconds = DefaultMetricsHealthSnapshotIntervalInMilliseconds;
        RequestCacheEnabled = DefaultRequestCacheEnabled;
        RequestLogEnabled = DefaultRequestLogEnabled;
    }

    private ExecutionIsolationStrategy GetIsolationStrategy(IHystrixCommandKey key)
    {
        string isolation = GetString(HystrixCommandPrefix, key.Name, "execution.isolation.strategy", DefaultIsolationStrategy.ToString(),
            defaults?.ExecutionIsolationStrategy.ToString());

        if (ExecutionIsolationStrategy.Thread.ToString().Equals(isolation, StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionIsolationStrategy.Thread;
        }

        if (ExecutionIsolationStrategy.Semaphore.ToString().Equals(isolation, StringComparison.OrdinalIgnoreCase))
        {
            return ExecutionIsolationStrategy.Semaphore;
        }

        throw new ArgumentOutOfRangeException("execution.isolation.strategy");
    }

    private string GetThreadPoolKeyOverride(string prefix, string key, string property, string globalDefault, string instanceDefaultFromCode)
    {
        string result = globalDefault;
        result = instanceDefaultFromCode ?? result; // instance default from code
        result = Dynamic != null ? Dynamic.GetString($"{prefix}:{key}:{property}", result) : result; // dynamic instance value
        return result;
    }
}
