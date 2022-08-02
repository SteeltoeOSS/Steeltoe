// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics;

public class HystrixMetricsPublisherFactory
{
    private static HystrixMetricsPublisherFactory _singleton = new();

    internal ConcurrentDictionary<string, IHystrixMetricsPublisherCommand> CommandPublishers { get; } = new();

    internal ConcurrentDictionary<string, IHystrixMetricsPublisherThreadPool> ThreadPoolPublishers { get; } = new();

    internal ConcurrentDictionary<string, IHystrixMetricsPublisherCollapser> CollapserPublishers { get; } = new();

    internal HystrixMetricsPublisherFactory()
    {
    }

    public static IHystrixMetricsPublisherThreadPool CreateOrRetrievePublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey,
        HystrixThreadPoolMetrics metrics, IHystrixThreadPoolOptions properties)
    {
        return _singleton.GetPublisherForThreadPool(threadPoolKey, metrics, properties);
    }

    public static IHystrixMetricsPublisherCommand CreateOrRetrievePublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner,
        HystrixCommandMetrics metrics, ICircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
    {
        return _singleton.GetPublisherForCommand(commandKey, commandOwner, metrics, circuitBreaker, properties);
    }

    public static IHystrixMetricsPublisherCollapser CreateOrRetrievePublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics,
        IHystrixCollapserOptions properties)
    {
        return _singleton.GetPublisherForCollapser(collapserKey, metrics, properties);
    }

    public static void Reset()
    {
        _singleton = new HystrixMetricsPublisherFactory();
        _singleton.CommandPublishers.Clear();
        _singleton.ThreadPoolPublishers.Clear();
        _singleton.CollapserPublishers.Clear();
    }

    internal IHystrixMetricsPublisherCommand GetPublisherForCommand(IHystrixCommandKey commandKey, IHystrixCommandGroupKey commandOwner,
        HystrixCommandMetrics metrics, ICircuitBreaker circuitBreaker, IHystrixCommandOptions properties)
    {
        return CommandPublishers.GetOrAddEx(commandKey.Name, _ =>
        {
            IHystrixMetricsPublisherCommand newPublisher =
                HystrixPlugins.MetricsPublisher.GetMetricsPublisherForCommand(commandKey, commandOwner, metrics, circuitBreaker, properties);

            newPublisher.Initialize();
            return newPublisher;
        });
    }

    internal IHystrixMetricsPublisherThreadPool GetPublisherForThreadPool(IHystrixThreadPoolKey threadPoolKey, HystrixThreadPoolMetrics metrics,
        IHystrixThreadPoolOptions properties)
    {
        return ThreadPoolPublishers.GetOrAddEx(threadPoolKey.Name, _ =>
        {
            IHystrixMetricsPublisherThreadPool publisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForThreadPool(threadPoolKey, metrics, properties);
            publisher.Initialize();
            return publisher;
        });
    }

    internal IHystrixMetricsPublisherCollapser GetPublisherForCollapser(IHystrixCollapserKey collapserKey, HystrixCollapserMetrics metrics,
        IHystrixCollapserOptions properties)
    {
        return CollapserPublishers.GetOrAddEx(collapserKey.Name, _ =>
        {
            IHystrixMetricsPublisherCollapser publisher = HystrixPlugins.MetricsPublisher.GetMetricsPublisherForCollapser(collapserKey, metrics, properties);
            publisher.Initialize();
            return publisher;
        });
    }
}
