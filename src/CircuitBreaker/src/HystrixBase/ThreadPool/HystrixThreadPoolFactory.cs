// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.ThreadPool;

internal static class HystrixThreadPoolFactory
{
    private static readonly object ShutdownLock = new();

    internal static ConcurrentDictionary<string, IHystrixThreadPool> ThreadPools { get; } = new();

    internal static IHystrixThreadPool GetInstance(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions propertiesBuilder)
    {
        // get the key to use instead of using the object itself so that if people forget to implement equals/hashcode things will still work
        string key = threadPoolKey.Name;

        // if we get here this is the first time so we need to initialize
        return ThreadPools.GetOrAddEx(key, _ => new HystrixThreadPoolDefault(threadPoolKey, propertiesBuilder));
    }

    internal static void Shutdown()
    {
        lock (ShutdownLock)
        {
            foreach (IHystrixThreadPool pool in ThreadPools.Values)
            {
                pool.Dispose();
            }

            ThreadPools.Clear();
        }
    }
}
