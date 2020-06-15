// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker
{
    public static class HystrixCircuitBreakerFactory
    {
        private static ConcurrentDictionary<string, ICircuitBreaker> circuitBreakersByCommand = new ConcurrentDictionary<string, ICircuitBreaker>();

        public static ICircuitBreaker GetInstance(IHystrixCommandKey key, IHystrixCommandGroupKey group, IHystrixCommandOptions options, HystrixCommandMetrics metrics)
        {
            return circuitBreakersByCommand.GetOrAddEx(key.Name, (k) => new HystrixCircuitBreakerImpl(key, group, options, metrics));
        }

        public static ICircuitBreaker GetInstance(IHystrixCommandKey key)
        {
            circuitBreakersByCommand.TryGetValue(key.Name, out ICircuitBreaker previouslyCached);
            return previouslyCached;
        }

        internal static void Reset()
        {
            circuitBreakersByCommand.Clear();
        }
    }
}
