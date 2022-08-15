// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix;

public static class HystrixCounters
{
    private static readonly AtomicInteger ConcurrentThreadsExecuting = new(0);

    public static int GlobalConcurrentThreadsExecuting => ConcurrentThreadsExecuting.Value;

    public static int CommandCount => HystrixCommandKeyDefault.CommandCount;

    public static int ThreadPoolCount => HystrixThreadPoolKeyDefault.ThreadPoolCount;

    public static int GroupCount => HystrixCommandGroupKeyDefault.GroupCount;

    internal static int IncrementGlobalConcurrentThreads()
    {
        return ConcurrentThreadsExecuting.IncrementAndGet();
    }

    internal static int DecrementGlobalConcurrentThreads()
    {
        return ConcurrentThreadsExecuting.DecrementAndGet();
    }
}
