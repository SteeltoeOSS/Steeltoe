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

using System;

namespace Steeltoe.Common.Transaction
{
    // TODO: Move this to common
    public abstract class ResourceHolderSupport : IResourceHolder
    {
        private int _referenceCount = 0;

        public bool SynchronizedWithTransaction { get; set; }

        public bool RollbackOnly { get; set; }

        public bool HasTimeout
        {
            get
            {
                return Deadline.HasValue;
            }
        }

        public DateTime? Deadline { get; private set; }

        public bool IsOpen => _referenceCount > 0;

        public bool IsVoid { get; private set; }

        public int GetTimetoLiveInSeconds()
        {
            var diff = ((double)GetTimeToLiveInMillis()) / 1000;
            var secs = (int)Math.Ceiling(diff);
            CheckTransactionTimeout(secs <= 0);
            return secs;
        }

        public long GetTimeToLiveInMillis()
        {
            if (!Deadline.HasValue)
            {
                throw new InvalidOperationException("No timeout specified for this resource holder");
            }

            var timeToLive = (Deadline.Value.Ticks - DateTime.Now.Ticks) / TimeSpan.TicksPerMillisecond;
            CheckTransactionTimeout(timeToLive <= 0);
            return timeToLive;
        }

        public void SetTimeoutInSeconds(int seconds)
        {
            Deadline = DateTime.Now + TimeSpan.FromSeconds(seconds);
        }

        public void SetTimeoutInMillis(long milliSeconds)
        {
            Deadline = DateTime.Now + TimeSpan.FromMilliseconds(milliSeconds);
        }

        public void Requested()
        {
            _referenceCount++;
        }

        public void Released()
        {
            _referenceCount--;
        }

        public void Clear()
        {
            SynchronizedWithTransaction = false;
            RollbackOnly = false;
            Deadline = null;
        }

        public void Reset()
        {
            Clear();
            _referenceCount = 0;
        }

        public void Unbound()
        {
            IsVoid = true;
        }

        private void CheckTransactionTimeout(bool deadlineReached)
        {
            if (deadlineReached)
            {
                RollbackOnly = true;
                throw new TransactionTimedOutException("Transaction timed out: deadline was " + Deadline);
            }
        }
    }
}
