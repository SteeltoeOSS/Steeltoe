﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HealthCounts
    {
        internal HealthCounts(long total, long error)
        {
            TotalRequests = total;
            ErrorCount = error;
            if (TotalRequests > 0)
            {
                ErrorPercentage = (int)((ErrorCount * 100) / TotalRequests);
            }
            else
            {
                ErrorPercentage = 0;
            }
        }

        public long TotalRequests { get; }

        public long ErrorCount { get; }

        public int ErrorPercentage { get; }

        public HealthCounts Plus(long[] eventTypeCounts)
        {
            var updatedTotalCount = TotalRequests;
            var updatedErrorCount = ErrorCount;

            var successCount = eventTypeCounts[(int)HystrixEventType.SUCCESS];
            var failureCount = eventTypeCounts[(int)HystrixEventType.FAILURE];
            var timeoutCount = eventTypeCounts[(int)HystrixEventType.TIMEOUT];
            var threadPoolRejectedCount = eventTypeCounts[(int)HystrixEventType.THREAD_POOL_REJECTED];
            var semaphoreRejectedCount = eventTypeCounts[(int)HystrixEventType.SEMAPHORE_REJECTED];

            updatedTotalCount += successCount + failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount;
            updatedErrorCount += failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount;
            return new HealthCounts(updatedTotalCount, updatedErrorCount);
        }

        public static HealthCounts Empty { get; } = new HealthCounts(0, 0);

        public override string ToString()
        {
            return $"HealthCounts[{ErrorCount} / {TotalRequests} : {ErrorPercentage}%]";
        }
    }
}
