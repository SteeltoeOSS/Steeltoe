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

namespace Steeltoe.Common.Transaction
{
    public class TransactionTemplate : DefaultTransactionDefinition
    {
        private ILogger _logger;

        public TransactionTemplate(ILogger logger = null)
        {
            _logger = logger;
        }

        public TransactionTemplate(IPlatformTransactionManager transactionManager, ILogger logger = null)
        {
            _logger = logger;
            TransactionManager = transactionManager;
        }

        public TransactionTemplate(IPlatformTransactionManager transactionManager, ITransactionDefinition transactionDefinition, ILogger logger = null)
            : base(transactionDefinition)
        {
            _logger = logger;
            TransactionManager = transactionManager;
        }

        public IPlatformTransactionManager TransactionManager { get; set; }

        public T Execute<T>(Func<ITransactionStatus, T> action)
        {
            if (TransactionManager == null)
            {
                throw new InvalidOperationException("No PlatformTransactionManager set");
            }
            else
            {
                var status = TransactionManager.GetTransaction(this);
                T result;
                try
                {
                    result = action(status);
                }
                catch (Exception ex)
                {
                    // Transactional code threw application exception -> rollback
                    RollbackOnException(status, ex);
                    throw;
                }

                // catch (Throwable ex)
                //    {
                //        // Transactional code threw unexpected exception -> rollback
                //        rollbackOnException(status, ex);
                //        throw new UndeclaredThrowableException(ex, "TransactionCallback threw undeclared checked exception");
                //    }
                TransactionManager.Commit(status);
                return result;
            }
        }

        public override bool Equals(object other)
        {
            return this == other ||
                (base.Equals(other) &&
                (!(other is TransactionTemplate) || TransactionManager == ((TransactionTemplate)other).TransactionManager));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private void RollbackOnException(ITransactionStatus status, Exception ex)
        {
            if (TransactionManager == null)
            {
                throw new InvalidOperationException("No PlatformTransactionManager set");
            }

            _logger?.LogDebug("Initiating transaction rollback on application exception", ex);
            try
            {
                TransactionManager.Rollback(status);
            }
            catch (TransactionSystemException ex2)
            {
                _logger?.LogError("Application exception overridden by rollback exception", ex);
                ex2.InitApplicationException(ex);
                throw;
            }
            catch (Exception ex2)
            {
                _logger?.LogError("Application exception overridden by rollback exception", ex);
                throw;
            }
        }
    }
}
