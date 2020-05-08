// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
