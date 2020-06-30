// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Transaction
{
    public static class TransactionSynchronizationUtils
    {
        public static void TriggerBeforeCommit(bool readOnly)
        {
            foreach (var synchronization in TransactionSynchronizationManager.GetSynchronizations())
            {
                synchronization.BeforeCommit(readOnly);
            }
        }

        public static void TriggerBeforeCompletion(ILogger logger = null)
        {
            foreach (var synchronization in TransactionSynchronizationManager.GetSynchronizations())
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
                foreach (var synchronization in synchronizations)
                {
                    synchronization.AfterCommit();
                }
            }
        }

        public static void InvokeAfterCompletion(List<ITransactionSynchronization> synchronizations, int completionStatus, ILogger logger = null)
        {
            if (synchronizations != null)
            {
                foreach (var synchronization in synchronizations)
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
}
