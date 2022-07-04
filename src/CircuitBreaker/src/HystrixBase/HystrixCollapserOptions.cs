// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCollapserOptions : HystrixBaseOptions, IHystrixCollapserOptions
{
    internal const int DefaultMaxRequestsInBatch = int.MaxValue;
    internal const int DefaultTimerDelayInMilliseconds = 10;
    internal const bool DefaultRequestCacheEnabled = true;
    internal const int DefaultMetricsRollingStatisticalWindow = 10000; // default => statisticalWindow: 10000 = 10 seconds (and default of 10 buckets so each bucket is 1 second)
    internal const int DefaultMetricsRollingStatisticalWindowBuckets = 10; // default => statisticalWindowBuckets: 10 = 10 buckets in a 10 second window so each bucket is 1 second
    internal const bool DefaultMetricsRollingPercentileEnabled = true;
    internal const int DefaultMetricsRollingPercentileWindow = 60000; // default to 1 minute for RollingPercentile
    internal const int DefaultMetricsRollingPercentileWindowBuckets = 6; // default to 6 buckets (10 seconds each in 60 second window)
    internal const int DefaultMetricsRollingPercentileBucketSize = 100; // default to 100 values max per bucket

    protected const string HystrixCollapserPrefix = "hystrix:collapser";
    private readonly IHystrixCollapserOptions _defaults;

    public HystrixCollapserOptions(IHystrixCollapserKey collapserKey, IHystrixCollapserOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : this(collapserKey, RequestCollapserScope.Request, defaults, dynamic)
    {
    }

    public HystrixCollapserOptions(IHystrixCollapserKey key, RequestCollapserScope scope, IHystrixCollapserOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : base(dynamic)
    {
        CollapserKey = key;
        Scope = scope;
        _defaults = defaults;

        MaxRequestsInBatch = GetInteger(HystrixCollapserPrefix, key.Name, "maxRequestsInBatch", DefaultMaxRequestsInBatch, defaults?.MaxRequestsInBatch);
        TimerDelayInMilliseconds = GetInteger(HystrixCollapserPrefix, key.Name, "timerDelayInMilliseconds", DefaultTimerDelayInMilliseconds, defaults?.TimerDelayInMilliseconds);
        RequestCacheEnabled = GetBoolean(HystrixCollapserPrefix, key.Name, "requestCache.enabled", DefaultRequestCacheEnabled, defaults?.RequestCacheEnabled);
        MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HystrixCollapserPrefix, key.Name, "metrics.rollingStats.timeInMilliseconds", DefaultMetricsRollingStatisticalWindow, defaults?.MetricsRollingStatisticalWindowInMilliseconds);
        MetricsRollingStatisticalWindowBuckets = GetInteger(HystrixCollapserPrefix, key.Name, "metrics.rollingStats.numBuckets", DefaultMetricsRollingStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets);
        MetricsRollingPercentileEnabled = GetBoolean(HystrixCollapserPrefix, key.Name, "metrics.rollingPercentile.enabled", DefaultMetricsRollingPercentileEnabled, defaults?.MetricsRollingPercentileEnabled);
        MetricsRollingPercentileWindowInMilliseconds = GetInteger(HystrixCollapserPrefix, key.Name, "metrics.rollingPercentile.timeInMilliseconds", DefaultMetricsRollingPercentileWindow, defaults?.MetricsRollingPercentileWindowInMilliseconds);
        MetricsRollingPercentileWindowBuckets = GetInteger(HystrixCollapserPrefix, key.Name, "metrics.rollingPercentile.numBuckets", DefaultMetricsRollingPercentileWindowBuckets, defaults?.MetricsRollingPercentileWindowBuckets);
        MetricsRollingPercentileBucketSize = GetInteger(HystrixCollapserPrefix, key.Name, "metrics.rollingPercentile.bucketSize", DefaultMetricsRollingPercentileBucketSize, defaults?.MetricsRollingPercentileBucketSize);
    }

    public IHystrixCollapserKey CollapserKey { get; set; }

    public RequestCollapserScope Scope { get; set; }

    public bool RequestCacheEnabled { get; set; }

    public int MaxRequestsInBatch { get; set; }

    public int TimerDelayInMilliseconds { get; set; }

    public int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

    public int MetricsRollingStatisticalWindowBuckets { get; set; }

    public bool MetricsRollingPercentileEnabled { get; set; }

    public int MetricsRollingPercentileWindowInMilliseconds { get; set; }

    public int MetricsRollingPercentileWindowBuckets { get; set; }

    public int MetricsRollingPercentileBucketSize { get; set; }
}
