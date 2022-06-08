// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Transaction;

public class DefaultTransactionStatus : AbstractTransactionStatus
{
    private ILogger _logger;

    public DefaultTransactionStatus(object transaction, bool newTransaction, bool newSynchronization, bool readOnly, object suspendedResources, ILogger logger)
    {
        Transaction = transaction;
        NewTransaction = newTransaction;
        IsNewSynchronization = newSynchronization;
        IsReadOnly = readOnly;
        _logger = logger;
        SuspendedResources = suspendedResources;
    }

    public object Transaction { get; }

    public bool HasTransaction => Transaction != null;

    public bool NewTransaction { get; }

    public bool IsNewSynchronization { get; }

    public bool IsReadOnly { get; }

    public object SuspendedResources { get; }

    public override bool IsNewTransaction => HasTransaction && NewTransaction;

    public bool IsTransactionSavepointManager => Transaction is ISavepointManager;

    public override void Flush()
    {
        if (Transaction is ISmartTransactionObject transactionObject)
        {
            transactionObject.Flush();
        }
    }

    public override bool IsGlobalRollbackOnly
    {
        get => Transaction is ISmartTransactionObject transactionObject && transactionObject.IsRollbackOnly;
        set => base.IsGlobalRollbackOnly = value;
    }

    protected override ISavepointManager GetSavepointManager()
    {
        var transaction = Transaction;
        if (transaction is not ISavepointManager savepointManager)
        {
            throw new NestedTransactionNotSupportedException($"Transaction object [{Transaction}] does not support savepoints");
        }

        return savepointManager;
    }
}
