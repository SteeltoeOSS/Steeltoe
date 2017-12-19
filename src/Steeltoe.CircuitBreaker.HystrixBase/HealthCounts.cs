// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
        private readonly long totalCount;
        private readonly long errorCount;
        private readonly int errorPercentage;

        internal HealthCounts(long total, long error)
        {
            this.totalCount = total;
            this.errorCount = error;
            if (totalCount > 0)
            {
                this.errorPercentage = (int)((errorCount * 100) / totalCount);
            }
            else
            {
                this.errorPercentage = 0;
            }
        }

        private static readonly HealthCounts EMPTY = new HealthCounts(0, 0);

        public long TotalRequests
        {
            get { return totalCount; }
        }

        public long ErrorCount
        {
            get { return errorCount; }
        }

        public int ErrorPercentage
        {
            get { return errorPercentage; }
        }

        public HealthCounts Plus(long[] eventTypeCounts)
        {
            long updatedTotalCount = totalCount;
            long updatedErrorCount = errorCount;

            long successCount = eventTypeCounts[(int)HystrixEventType.SUCCESS];
            long failureCount = eventTypeCounts[(int)HystrixEventType.FAILURE];
            long timeoutCount = eventTypeCounts[(int)HystrixEventType.TIMEOUT];
            long threadPoolRejectedCount = eventTypeCounts[(int)HystrixEventType.THREAD_POOL_REJECTED];
            long semaphoreRejectedCount = eventTypeCounts[(int)HystrixEventType.SEMAPHORE_REJECTED];

            updatedTotalCount += successCount + failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount;
            updatedErrorCount += failureCount + timeoutCount + threadPoolRejectedCount + semaphoreRejectedCount;
            return new HealthCounts(updatedTotalCount, updatedErrorCount);
        }

        public static HealthCounts Empty
        {
            get { return EMPTY; }
        }

        public override string ToString()
        {
            return "HealthCounts[" + errorCount + " / " + totalCount + " : " + ErrorPercentage + "%]";
        }
    }
}
