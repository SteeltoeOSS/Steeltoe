// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample
{
    public class HystrixCommandUtilization
    {
        public HystrixCommandUtilization(int concurrentCommandCount)
        {
            ConcurrentCommandCount = concurrentCommandCount;
        }

        public static HystrixCommandUtilization Sample(HystrixCommandMetrics commandMetrics)
        {
            return new HystrixCommandUtilization(commandMetrics.CurrentConcurrentExecutionCount);
        }

        public int ConcurrentCommandCount { get; }
    }
}
