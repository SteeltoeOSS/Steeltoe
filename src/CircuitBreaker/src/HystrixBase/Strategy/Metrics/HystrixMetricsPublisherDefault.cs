// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Metrics
{
    public class HystrixMetricsPublisherDefault : HystrixMetricsPublisher
    {
        private static HystrixMetricsPublisherDefault instance = new HystrixMetricsPublisherDefault();

        public static HystrixMetricsPublisher GetInstance()
        {
            return instance;
        }

        private HystrixMetricsPublisherDefault()
        {
        }
    }
}
