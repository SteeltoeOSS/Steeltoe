// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HealthCounts
{
    public static HealthCounts Empty { get; } = new(0, 0);

    public long TotalRequests { get; }

    public long ErrorCount { get; }

    public int ErrorPercentage { get; }

    internal HealthCounts(long total, long error)
    {
        TotalRequests = total;
        ErrorCount = error;

        if (TotalRequests > 0)
        {
            ErrorPercentage = (int)(ErrorCount * 100 / TotalRequests);
        }
        else
        {
            ErrorPercentage = 0;
        }
    }

    public HealthCounts Plus(long[] eventTypeCounts)
    {
        long updatedTotalCount = TotalRequests;
        long updatedErrorCount = ErrorCount;

        long successCount = eventTypeCounts[(int)HystrixEventType.Success];
        long failureCount = eventTypeCounts[(int)HystrixEventType.Failure];
        long timeoutCount = eventTypeCounts[(int)HystrixEventType.Timeout];
        long threadPoolRejectedCount = eventTypeCounts[(int)HystrixEventType.ThreadPoolRejected];
        long semaphoreRejectedCount = eventTypeCounts[(int)HystrixEventType.SemaphoreRejected];

        updatedTotalCount += successCount + failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount;
        updatedErrorCount += failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount;
        return new HealthCounts(updatedTotalCount, updatedErrorCount);
    }

    public override string ToString()
    {
        return $"HealthCounts[{ErrorCount} / {TotalRequests} : {ErrorPercentage}%]";
    }
}
