// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public class ResourceHolderSynchronization<THolder, TKey> : ITransactionSynchronization
    where THolder : IResourceHolder
{
    private readonly THolder _resourceHolder;

    private readonly TKey _resourceKey;

    private volatile bool _holderActive;

    public ResourceHolderSynchronization(THolder resourceHolder, TKey resourceKey)
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
            bool releaseNecessary = false;

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
            CleanupResource(_resourceHolder, _resourceKey, status == AbstractTransactionSynchronization.StatusCommitted);
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

    protected virtual bool ShouldReleaseAfterCompletion(THolder resourceHolder)
    {
        return !ShouldReleaseBeforeCompletion();
    }

    protected virtual void FlushResource(THolder resourceHolder)
    {
    }

    protected virtual void ProcessResourceAfterCommit(THolder resourceHolder)
    {
    }

    protected virtual void ReleaseResource(THolder resourceHolder, TKey resourceKey)
    {
    }

    protected virtual void CleanupResource(THolder resourceHolder, TKey resourceKey, bool committed)
    {
    }
}
