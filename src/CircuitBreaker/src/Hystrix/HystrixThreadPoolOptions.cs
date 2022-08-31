// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixThreadPoolOptions : HystrixBaseOptions, IHystrixThreadPoolOptions
{
    internal const int DefaultCoreSize = 10; // core size of thread pool
    internal const int DefaultMaximumSize = 10; // maximum size of thread pool
    internal const int DefaultKeepAliveTimeMinutes = 1; // minutes to keep a thread alive

    internal const int
        DefaultMaxQueueSize = -1; // size of queue (this can't be dynamically changed so we use 'queueSizeRejectionThreshold' to artificially limit and reject)

    // -1 turns it off and makes us use SynchronousQueue
    internal const bool
        DefaultAllowMaximumSizeToDivergeFromCoreSize = false; // should the maximumSize configuration value get read and used in configuring the threadPool

    // turning this on should be a conscious decision by the user, so we default it to false
    internal const int DefaultQueueSizeRejectionThreshold = 5; // number of items in queue
    internal const int DefaultThreadPoolRollingNumberStatisticalWindow = 10000; // milliseconds for rolling number
    internal const int DefaultThreadPoolRollingNumberStatisticalWindowBuckets = 10; // number of buckets in rolling number (10 1-second buckets)

    protected const string HystrixThreadpoolPrefix = "hystrix:threadpool";

    protected IHystrixThreadPoolOptions defaults;

    public IHystrixThreadPoolKey ThreadPoolKey { get; internal set; }

    public virtual int CoreSize { get; set; }

    public virtual int MaximumSize { get; set; }

    public virtual int KeepAliveTimeMinutes { get; set; }

    public virtual int MaxQueueSize { get; set; }

    public virtual int QueueSizeRejectionThreshold { get; set; }

    public virtual bool AllowMaximumSizeToDivergeFromCoreSize { get; set; }

    public virtual int MetricsRollingStatisticalWindowInMilliseconds { get; set; }

    public virtual int MetricsRollingStatisticalWindowBuckets { get; set; }

    public HystrixThreadPoolOptions(IHystrixThreadPoolKey key, IHystrixThreadPoolOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : this(defaults, dynamic)
    {
        ThreadPoolKey = key;

        AllowMaximumSizeToDivergeFromCoreSize = GetBoolean(HystrixThreadpoolPrefix, key.Name, "allowMaximumSizeToDivergeFromCoreSize",
            DefaultAllowMaximumSizeToDivergeFromCoreSize, defaults?.AllowMaximumSizeToDivergeFromCoreSize);

        CoreSize = GetInteger(HystrixThreadpoolPrefix, key.Name, "coreSize", DefaultCoreSize, defaults?.CoreSize);
        MaximumSize = GetInteger(HystrixThreadpoolPrefix, key.Name, "maximumSize", DefaultMaximumSize, defaults?.MaximumSize);

        KeepAliveTimeMinutes = GetInteger(HystrixThreadpoolPrefix, key.Name, "keepAliveTimeMinutes", DefaultKeepAliveTimeMinutes,
            defaults?.KeepAliveTimeMinutes);

        MaxQueueSize = GetInteger(HystrixThreadpoolPrefix, key.Name, "maxQueueSize", DefaultMaxQueueSize, defaults?.MaxQueueSize);

        QueueSizeRejectionThreshold = GetInteger(HystrixThreadpoolPrefix, key.Name, "queueSizeRejectionThreshold", DefaultQueueSizeRejectionThreshold,
            defaults?.QueueSizeRejectionThreshold);

        MetricsRollingStatisticalWindowInMilliseconds = GetInteger(HystrixThreadpoolPrefix, key.Name, "metrics.rollingStats.timeInMilliseconds",
            DefaultThreadPoolRollingNumberStatisticalWindow, defaults?.MetricsRollingStatisticalWindowInMilliseconds);

        MetricsRollingStatisticalWindowBuckets = GetInteger(HystrixThreadpoolPrefix, key.Name, "metrics.rollingPercentile.numBuckets",
            DefaultThreadPoolRollingNumberStatisticalWindowBuckets, defaults?.MetricsRollingStatisticalWindowBuckets);
    }

    internal HystrixThreadPoolOptions(IHystrixThreadPoolOptions defaults = null, IHystrixDynamicOptions dynamic = null)
        : base(dynamic)
    {
        this.defaults = defaults;
        ThreadPoolKey = null;

        AllowMaximumSizeToDivergeFromCoreSize = DefaultAllowMaximumSizeToDivergeFromCoreSize;
        CoreSize = DefaultCoreSize;
        MaximumSize = DefaultMaximumSize;
        KeepAliveTimeMinutes = DefaultKeepAliveTimeMinutes;
        MaxQueueSize = DefaultMaxQueueSize;
        QueueSizeRejectionThreshold = DefaultQueueSizeRejectionThreshold;
        MetricsRollingStatisticalWindowInMilliseconds = DefaultThreadPoolRollingNumberStatisticalWindow;
        MetricsRollingStatisticalWindowBuckets = DefaultThreadPoolRollingNumberStatisticalWindowBuckets;
    }
}
