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
    public abstract class AbstractTransactionStatus : ITransactionStatus
    {
        public abstract bool IsNewTransaction { get; }

        public virtual void SetRollbackOnly()
        {
            IsLocalRollbackOnly = true;
        }

        public virtual bool IsRollbackOnly
        {
            get
            {
                return IsLocalRollbackOnly || IsGlobalRollbackOnly;
            }
        }

        public virtual bool IsLocalRollbackOnly { get; set; }

        public virtual bool IsGlobalRollbackOnly { get; set; }

        public virtual bool IsCompleted { get; set; }

        public virtual object Savepoint { get; set; }

        public virtual bool HasSavepoint => Savepoint != null;

        public virtual void CreateAndHoldSavepoint()
        {
            Savepoint = GetSavepointManager().CreateSavepoint();
        }

        public virtual void RollbackToHeldSavepoint()
        {
            var savepoint = Savepoint;
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
            var savepoint = Savepoint;
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

        protected virtual ISavepointManager GetSavepointManager() => throw new NestedTransactionNotSupportedException("This transaction does not support savepoints");
    }
}
