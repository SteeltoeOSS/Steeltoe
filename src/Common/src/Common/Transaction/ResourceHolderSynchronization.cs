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

namespace Steeltoe.Common.Transaction
{
    // TODO: Move this to common
    public class ResourceHolderSynchronization<H, K> : ITransactionSynchronization
        where H : IResourceHolder
    {
        private readonly H _resourceHolder;

        private readonly K _resourceKey;

        private volatile bool _holderActive;

        public ResourceHolderSynchronization(H resourceHolder, K resourceKey)
        {
            _resourceHolder = resourceHolder;
            _resourceKey = resourceKey;
            _holderActive = true;
        }

        public virtual void Suspend()
        {
            if (_holderActive)
            {
                TransactionSynchronizationManager.UnbindResource(_resourceKey);
            }
        }

        public virtual void Resume()
        {
            if (_holderActive)
            {
                TransactionSynchronizationManager.BindResource(_resourceKey, _resourceHolder);
            }
        }

        public virtual void Flush()
        {
            FlushResource(_resourceHolder);
        }

        public virtual void BeforeCommit(bool readOnly)
        {
            // Nothing to do
        }

        public virtual void BeforeCompletion()
        {
            if (ShouldUnbindAtCompletion())
            {
                TransactionSynchronizationManager.UnbindResource(_resourceKey);
                _holderActive = false;
                if (ShouldReleaseBeforeCompletion())
                {
                    ReleaseResource(_resourceHolder, _resourceKey);
                }
            }
        }

        public virtual void AfterCommit()
        {
            if (!ShouldReleaseBeforeCompletion())
            {
                ProcessResourceAfterCommit(_resourceHolder);
            }
        }

        public virtual void AfterCompletion(int status)
        {
            if (ShouldUnbindAtCompletion())
            {
                var releaseNecessary = false;
                if (_holderActive)
                {
                    // The thread-bound resource holder might not be available anymore,
                    // since afterCompletion might get called from a different thread.
                    _holderActive = false;
                    TransactionSynchronizationManager.UnbindResourceIfPossible(_resourceKey);
                    _resourceHolder.Unbound();
                    releaseNecessary = true;
                }
                else
                {
                    releaseNecessary = ShouldReleaseAfterCompletion(_resourceHolder);
                }

                if (releaseNecessary)
                {
                    ReleaseResource(_resourceHolder, _resourceKey);
                }
            }
            else
            {
                // Probably a pre-bound resource...
                CleanupResource(_resourceHolder, _resourceKey, status == AbstractTransactionSynchronization.STATUS_COMMITTED);
            }

            _resourceHolder.Reset();
        }

        protected virtual bool ShouldUnbindAtCompletion()
        {
            return true;
        }

        protected virtual bool ShouldReleaseBeforeCompletion()
        {
            return true;
        }

        protected virtual bool ShouldReleaseAfterCompletion(H resourceHolder)
        {
            return !ShouldReleaseBeforeCompletion();
        }

        protected virtual void FlushResource(H resourceHolder)
        {
        }

        protected virtual void ProcessResourceAfterCommit(H resourceHolder)
        {
        }

        protected virtual void ReleaseResource(H resourceHolder, K resourceKey)
        {
        }

        protected virtual void CleanupResource(H resourceHolder, K resourceKey, bool committed)
        {
        }
    }
}
