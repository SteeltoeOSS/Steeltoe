// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public abstract class ResourceHolderSupport : IResourceHolder
{
    private int _referenceCount;

    public bool SynchronizedWithTransaction { get; set; }

    public bool RollbackOnly { get; set; }

    public bool HasTimeout => Deadline.HasValue;

    public DateTime? Deadline { get; private set; }

    public bool IsOpen => _referenceCount > 0;

    public bool IsVoid { get; private set; }

    public int GetTimeToLiveInSeconds()
    {
        double diff = (double)GetTimeToLiveInMillis() / 1000;
        int secs = (int)Math.Ceiling(diff);
        CheckTransactionTimeout(secs <= 0);
        return secs;
    }

    public long GetTimeToLiveInMillis()
    {
        if (!Deadline.HasValue)
        {
            throw new InvalidOperationException("No timeout specified for this resource holder");
        }

        long timeToLive = (Deadline.Value.Ticks - DateTime.UtcNow.Ticks) / TimeSpan.TicksPerMillisecond;
        CheckTransactionTimeout(timeToLive <= 0);
        return timeToLive;
    }

    public void SetTimeoutInSeconds(int seconds)
    {
        Deadline = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
    }

    public void SetTimeoutInMillis(long milliseconds)
    {
        Deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(milliseconds);
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
            throw new TransactionTimedOutException($"Transaction timed out: deadline was {Deadline}");
        }
    }
}
