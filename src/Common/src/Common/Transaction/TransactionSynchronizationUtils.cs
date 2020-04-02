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
                    logger?.LogError("TransactionSynchronization.beforeCompletion threw exception", ex);
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
                foreach (var synchronization in TransactionSynchronizationManager.GetSynchronizations())
                {
                    synchronization.AfterCommit();
                }
            }
        }

        public static void InvokeAfterCompletion(List<ITransactionSynchronization> synchronizations, int completionStatus, ILogger logger = null)
        {
            if (synchronizations != null)
            {
                foreach (var synchronization in TransactionSynchronizationManager.GetSynchronizations())
                {
                    try
                    {
                        synchronization.AfterCompletion(completionStatus);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("TransactionSynchronization.afterCompletion threw exception", ex);
                    }
                }
            }
        }
    }
}
