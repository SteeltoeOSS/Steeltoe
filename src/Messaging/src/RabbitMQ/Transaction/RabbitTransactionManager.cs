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
using Steeltoe.Common.Transaction;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Exceptions;
using System;

namespace Steeltoe.Messaging.Rabbit.Transaction
{
    public class RabbitTransactionManager : AbstractPlatformTransactionManager, IResourceTransactionManager
    {
        public RabbitTransactionManager()
        {
            TransactionSynchronization = SYNCHRONIZATION_NEVER;
        }

        public RabbitTransactionManager(IConnectionFactory connectionFactory, ILogger logger = null)
            : base(logger)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            ConnectionFactory = connectionFactory;
        }

        public IConnectionFactory ConnectionFactory { get; set; }

        public object ResourceFactory => ConnectionFactory;

        protected override object DoGetTransaction()
        {
            var txObject = new RabbitTransactionObject((RabbitResourceHolder)TransactionSynchronizationManager.GetResource(ConnectionFactory));
            return txObject;
        }

        protected override bool IsExistingTransaction(object transaction)
        {
            var txObject = (RabbitTransactionObject)transaction;
            return txObject.ResourceHolder != null;
        }

        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
            if (definition.IsolationLevel != AbstractTransactionDefinition.ISOLATION_DEFAULT)
            {
                throw new InvalidIsolationLevelException("AMQP does not support an isolation level concept");
            }

            var txObject = (RabbitTransactionObject)transaction;
            RabbitResourceHolder resourceHolder = null;
            try
            {
                resourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(ConnectionFactory, true);
                _logger?.LogDebug("Created AMQP transaction on channel [{channel}]", resourceHolder.GetChannel());

                txObject.ResourceHolder = resourceHolder;
                txObject.ResourceHolder.SynchronizedWithTransaction = true;
                var timeout = DetermineTimeout(definition);
                if (timeout != AbstractTransactionDefinition.TIMEOUT_DEFAULT)
                {
                    txObject.ResourceHolder.SetTimeoutInSeconds(timeout);
                }

                TransactionSynchronizationManager.BindResource(ConnectionFactory, txObject.ResourceHolder);
            }
            catch (RabbitException ex)
            {
                if (resourceHolder != null)
                {
                    ConnectionFactoryUtils.ReleaseResources(resourceHolder);
                }

                throw new CannotCreateTransactionException("Could not create AMQP transaction", ex);
            }
        }

        protected override object DoSuspend(object transaction)
        {
            var txObject = (RabbitTransactionObject)transaction;
            txObject.ResourceHolder = null;
            return TransactionSynchronizationManager.UnbindResource(ConnectionFactory);
        }

        protected override void DoResume(object transaction, object suspendedResources)
        {
            var conHolder = (RabbitResourceHolder)suspendedResources;
            TransactionSynchronizationManager.BindResource(ConnectionFactory, conHolder);
        }

        protected override void DoCommit(DefaultTransactionStatus status)
        {
            var txObject = (RabbitTransactionObject)status.Transaction;
            var resourceHolder = txObject.ResourceHolder;
            resourceHolder.CommitAll();
        }

        protected override void DoRollback(DefaultTransactionStatus status)
        {
            var txObject = (RabbitTransactionObject)status.Transaction;
            var resourceHolder = txObject.ResourceHolder;
            resourceHolder.RollbackAll();
        }

        protected override void DoSetRollbackOnly(DefaultTransactionStatus status)
        {
            var txObject = (RabbitTransactionObject)status.Transaction;
            txObject.ResourceHolder.RollbackOnly = true;
        }

        protected override void DoCleanupAfterCompletion(object transaction)
        {
            var txObject = (RabbitTransactionObject)transaction;
            TransactionSynchronizationManager.UnbindResource(ConnectionFactory);
            txObject.ResourceHolder.CloseAll();
            txObject.ResourceHolder.Clear();
        }

        private class RabbitTransactionObject : ISmartTransactionObject
        {
            public RabbitTransactionObject(RabbitResourceHolder rabbitResourceHolder)
            {
                ResourceHolder = rabbitResourceHolder;
            }

            public RabbitResourceHolder ResourceHolder { get; set; }

            public bool IsRollbackOnly => ResourceHolder.RollbackOnly;

            public void Flush()
            {
                // no-op
            }
        }
    }
}
