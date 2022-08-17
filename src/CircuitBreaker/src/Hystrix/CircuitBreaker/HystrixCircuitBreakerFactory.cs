// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;

public static class HystrixCircuitBreakerFactory
{
    private static readonly ConcurrentDictionary<string, ICircuitBreaker> CircuitBreakersByCommand = new();

    public static ICircuitBreaker GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey group, IHystrixCommandOptions options,
        HystrixCommandMetrics metrics)
    {
        return CircuitBreakersByCommand.GetOrAddEx(key.Name, _ => new HystrixCircuitBreakerImpl(options, metrics));
    }

    public static ICircuitBreaker GetInstance(IHystrixCommandKey key)
    {
        CircuitBreakersByCommand.TryGetValue(key.Name, out ICircuitBreaker previouslyCached);
        return previouslyCached;
    }

    internal static void Reset()
    {
        CircuitBreakersByCommand.Clear();
    }
}
