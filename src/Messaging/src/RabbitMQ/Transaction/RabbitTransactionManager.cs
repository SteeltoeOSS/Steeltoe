// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Transaction;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Transaction;

public class RabbitTransactionManager : AbstractPlatformTransactionManager, IResourceTransactionManager
{
    public IConnectionFactory ConnectionFactory { get; set; }

    public object ResourceFactory => ConnectionFactory;

    public RabbitTransactionManager()
    {
        TransactionSynchronization = SynchronizationNever;
    }

    public RabbitTransactionManager(IConnectionFactory connectionFactory, ILogger logger = null)
        : base(logger)
    {
        ArgumentGuard.NotNull(connectionFactory);

        ConnectionFactory = connectionFactory;
    }

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
        if (definition.IsolationLevel != AbstractTransactionDefinition.IsolationDefault)
        {
            throw new InvalidIsolationLevelException("AMQP does not support an isolation level concept");
        }

        var txObject = (RabbitTransactionObject)transaction;
        RabbitResourceHolder resourceHolder = null;

        try
        {
            resourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(ConnectionFactory, true);
            Logger?.LogDebug("Created AMQP transaction on channel [{channel}]", resourceHolder.GetChannel());

            txObject.ResourceHolder = resourceHolder;
            txObject.ResourceHolder.SynchronizedWithTransaction = true;
            int timeout = DetermineTimeout(definition);

            if (timeout != AbstractTransactionDefinition.TimeoutDefault)
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
        RabbitResourceHolder resourceHolder = txObject.ResourceHolder;
        resourceHolder.CommitAll();
    }

    protected override void DoRollback(DefaultTransactionStatus status)
    {
        var txObject = (RabbitTransactionObject)status.Transaction;
        RabbitResourceHolder resourceHolder = txObject.ResourceHolder;
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

    private sealed class RabbitTransactionObject : ISmartTransactionObject
    {
        public RabbitResourceHolder ResourceHolder { get; set; }

        public bool IsRollbackOnly => ResourceHolder.RollbackOnly;

        public RabbitTransactionObject(RabbitResourceHolder rabbitResourceHolder)
        {
            ResourceHolder = rabbitResourceHolder;
        }

        public void Flush()
        {
            // no-op
        }
    }
}
