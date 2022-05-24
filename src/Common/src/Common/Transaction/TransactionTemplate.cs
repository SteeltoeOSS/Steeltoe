// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public void Execute(Action<ITransactionStatus> action)
        {
            Execute<object>(s =>
            {
                action(s);
                return null;
            });
        }

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
            => this == other ||
                (base.Equals(other) && (other is not TransactionTemplate otherTemplate || TransactionManager == otherTemplate.TransactionManager));

        public override int GetHashCode() => base.GetHashCode();

        private void RollbackOnException(ITransactionStatus status, Exception ex)
        {
            if (TransactionManager == null)
            {
                throw new InvalidOperationException("No PlatformTransactionManager set");
            }

            _logger?.LogDebug(ex, "Initiating transaction rollback on application exception");
            try
            {
                TransactionManager.Rollback(status);
            }
            catch (TransactionSystemException ex2)
            {
                _logger?.LogError(ex, "Application exception overridden by rollback exception");
                ex2.InitApplicationException(ex);
                throw;
            }
            catch (Exception ex2)
            {
                _logger?.LogError(ex2, "Application exception overridden by rollback exception: {original}", ex);
                throw;
            }
        }
    }
}
