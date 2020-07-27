﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker
{
    public static class HystrixCircuitBreakerFactory
    {
        private static readonly ConcurrentDictionary<string, IHystrixCircuitBreaker> CircuitBreakersByCommand = new ConcurrentDictionary<string, IHystrixCircuitBreaker>();

        public static IHystrixCircuitBreaker GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey group, IHystrixCommandOptions options, HystrixCommandMetrics metrics)
        {
            return CircuitBreakersByCommand.GetOrAddEx(key.Name, (k) => new HystrixCircuitBreakerImpl(key, group, options, metrics));
        }

        public static IHystrixCircuitBreaker GetInstance(IHystrixCommandKey key)
        {
            CircuitBreakersByCommand.TryGetValue(key.Name, out var previouslyCached);
            return previouslyCached;
        }

        internal static void Reset()
        {
            CircuitBreakersByCommand.Clear();
        }
    }
}
