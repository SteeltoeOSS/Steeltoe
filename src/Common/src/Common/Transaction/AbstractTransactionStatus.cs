// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public abstract class AbstractTransactionStatus : ITransactionStatus
{
    public abstract bool IsNewTransaction { get; }

    public virtual bool IsRollbackOnly => IsLocalRollbackOnly || IsGlobalRollbackOnly;

    public virtual bool IsLocalRollbackOnly { get; set; }

    public virtual bool IsGlobalRollbackOnly { get; set; }

    public virtual bool IsCompleted { get; set; }

    public virtual object Savepoint { get; set; }

    public virtual bool HasSavepoint => Savepoint != null;

    public virtual void SetRollbackOnly()
    {
        IsLocalRollbackOnly = true;
    }

    public virtual void CreateAndHoldSavepoint()
    {
        Savepoint = GetSavepointManager().CreateSavepoint();
    }

    public virtual void RollbackToHeldSavepoint()
    {
        object savepoint = Savepoint;

        if (savepoint == null)
        {
            throw new TransactionUsageException("Cannot roll back to savepoint - no savepoint associated with current transaction");
        }

        GetSavepointManager().RollbackToSavepoint(savepoint);
        GetSavepointManager().ReleaseSavepoint(savepoint);
        Savepoint = null;
    }

    public virtual void ReleaseHeldSavepoint()
    {
        object savepoint = Savepoint;

        if (savepoint == null)
        {
            throw new TransactionUsageException("Cannot release savepoint - no savepoint associated with current transaction");
        }

        GetSavepointManager().ReleaseSavepoint(savepoint);
        Savepoint = null;
    }

    public virtual object CreateSavepoint()
    {
        return GetSavepointManager().CreateSavepoint();
    }

    public virtual void RollbackToSavepoint(object savepoint)
    {
        GetSavepointManager().RollbackToSavepoint(savepoint);
    }

    public virtual void ReleaseSavepoint(object savepoint)
    {
        GetSavepointManager().ReleaseSavepoint(savepoint);
    }

    public virtual void Flush()
    {
    }

    protected virtual ISavepointManager GetSavepointManager()
    {
        throw new NestedTransactionNotSupportedException("This transaction does not support savepoints");
    }
}
