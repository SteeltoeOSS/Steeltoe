// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.CircuitBreaker.HystrixBase.Util
{
    public static class HystrixShutdown
    {
        public static void ShutdownThreads()
        {
            CumulativeCommandEventCounterStream.Reset();
            CumulativeThreadPoolEventCounterStream.Reset();
            RollingCommandEventCounterStream.Reset();
            RollingThreadPoolEventCounterStream.Reset();
            RollingCollapserEventCounterStream.Reset();
            RollingCollapserEventCounterStream.Reset();
            HealthCountsStream.Reset();
            RollingCollapserBatchSizeDistributionStream.Reset();
            RollingCommandLatencyDistributionStream.Reset();
            RollingCommandUserLatencyDistributionStream.Reset();
            RollingCommandMaxConcurrencyStream.Reset();
            RollingThreadPoolMaxConcurrencyStream.Reset();
        }
    }
}
