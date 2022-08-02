// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Transaction;

public static class TransactionSynchronizationUtils
{
    public static void TriggerBeforeCommit(bool readOnly)
    {
        foreach (ITransactionSynchronization synchronization in TransactionSynchronizationManager.GetSynchronizations())
        {
            synchronization.BeforeCommit(readOnly);
        }
    }

    public static void TriggerBeforeCompletion(ILogger logger = null)
    {
        foreach (ITransactionSynchronization synchronization in TransactionSynchronizationManager.GetSynchronizations())
        {
            try
            {
                synchronization.BeforeCompletion();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "TransactionSynchronization.beforeCompletion threw exception");
            }
        }
    }

    public static void TriggerAfterCommit()
    {
        InvokeAfterCommit(TransactionSynchronizationManager.GetSynchronizations());
    }

    public static void InvokeAfterCommit(List<ITransactionSynchronization> synchronizations)
    {
        if (synchronizations != null)
        {
            foreach (ITransactionSynchronization synchronization in synchronizations)
            {
                synchronization.AfterCommit();
            }
        }
    }

    public static void TriggerAfterCompletion(int completionStatus)
    {
        List<ITransactionSynchronization> synchronizations = TransactionSynchronizationManager.GetSynchronizations();
        InvokeAfterCompletion(synchronizations, completionStatus);
    }

    public static void InvokeAfterCompletion(List<ITransactionSynchronization> synchronizations, int completionStatus, ILogger logger = null)
    {
        if (synchronizations != null)
        {
            foreach (ITransactionSynchronization synchronization in synchronizations)
            {
                try
                {
                    synchronization.AfterCompletion(completionStatus);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "TransactionSynchronization.afterCompletion threw exception");
                }
            }
        }
    }
}
