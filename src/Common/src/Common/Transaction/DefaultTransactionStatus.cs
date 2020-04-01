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

namespace Steeltoe.Common.Transaction
{
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

        public override bool IsNewTransaction => HasTransaction && IsNewTransaction;

        public bool IsTransactionSavepointManager => Transaction is ISavepointManager;

        public override void Flush()
        {
            if (Transaction is ISmartTransactionObject)
            {
                ((ISmartTransactionObject)Transaction).Flush();
            }
        }

        public override bool IsGlobalRollbackOnly
        {
            get
            {
                return (Transaction is ISmartTransactionObject) && ((ISmartTransactionObject)Transaction).IsRollbackOnly;
            }
            set => base.IsGlobalRollbackOnly = value;
        }

        protected override ISavepointManager GetSavepointManager()
        {
                var transaction = Transaction;
                if (!(transaction is ISavepointManager))
                {
                    throw new NestedTransactionNotSupportedException("Transaction object [" + Transaction + "] does not support savepoints");
                }

                return (ISavepointManager)Transaction;
        }
    }
}
