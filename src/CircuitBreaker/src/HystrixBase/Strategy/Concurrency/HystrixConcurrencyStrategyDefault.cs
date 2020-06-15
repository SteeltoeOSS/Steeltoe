// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public class HystrixConcurrencyStrategyDefault : HystrixConcurrencyStrategy
    {
        private static HystrixConcurrencyStrategyDefault instance = new HystrixConcurrencyStrategyDefault();

        public static HystrixConcurrencyStrategy GetInstance()
        {
            return instance;
        }

        private HystrixConcurrencyStrategyDefault()
        {
        }
    }
}
