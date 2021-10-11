// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options
{
    internal class HystrixOptionsStrategyDefault : HystrixOptionsStrategy
    {
        private static readonly HystrixOptionsStrategyDefault Instance = new ();

        private HystrixOptionsStrategyDefault()
        {
        }

        public static HystrixOptionsStrategy GetInstance()
        {
            return Instance;
        }
    }
}
