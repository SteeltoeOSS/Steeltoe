// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample;

public class HystrixUtilization
{
    public HystrixUtilization(
        Dictionary<IHystrixCommandKey, HystrixCommandUtilization> commandUtilizationMap,
        Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> threadPoolUtilizationMap)
    {
        CommandUtilizationMap = commandUtilizationMap;
        ThreadPoolUtilizationMap = threadPoolUtilizationMap;
    }

    public static HystrixUtilization From(
        Dictionary<IHystrixCommandKey, HystrixCommandUtilization> commandUtilizationMap,
        Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> threadPoolUtilizationMap)
    {
        return new HystrixUtilization(commandUtilizationMap, threadPoolUtilizationMap);
    }

    public Dictionary<IHystrixCommandKey, HystrixCommandUtilization> CommandUtilizationMap { get; }

    public Dictionary<IHystrixThreadPoolKey, HystrixThreadPoolUtilization> ThreadPoolUtilizationMap { get; }
}
