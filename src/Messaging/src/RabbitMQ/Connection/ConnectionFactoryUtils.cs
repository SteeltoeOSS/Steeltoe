// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Transaction;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public static class ConnectionFactoryUtils
{
    public static RabbitResourceHolder GetTransactionalResourceHolder(IConnectionFactory connectionFactory, bool synchedLocalTransactionAllowed, bool publisherConnectionIfPossible)
    {
        return DoGetTransactionalResourceHolder(connectionFactory, new RabbitResourceFactory(connectionFactory, synchedLocalTransactionAllowed, publisherConnectionIfPossible));
    }

    public static RabbitResourceHolder GetTransactionalResourceHolder(IConnectionFactory connectionFactory, bool synchedLocalTransactionAllowed)
    {
        return GetTransactionalResourceHolder(connectionFactory, synchedLocalTransactionAllowed, false);
    }

    public static bool IsChannelTransactional(RC.IModel channel, IConnectionFactory connectionFactory)
    {
        if (channel == null || connectionFactory == null)
        {
            return false;
        }

        var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
        return resourceHolder != null && resourceHolder.ContainsChannel(channel);
    }

    public static void ReleaseResources(RabbitResourceHolder resourceHolder)
    {
        if (resourceHolder == null || resourceHolder.SynchronizedWithTransaction)
        {
            return;
        }

        RabbitUtils.CloseChannel(resourceHolder.GetChannel());
        RabbitUtils.CloseConnection(resourceHolder.GetConnection());
    }

    public static RabbitResourceHolder BindResourceToTransaction(RabbitResourceHolder resourceHolder, IConnectionFactory connectionFactory, bool synched)
    {
        if (TransactionSynchronizationManager.HasResource(connectionFactory) || !TransactionSynchronizationManager.IsActualTransactionActive() || !synched)
        {
            return (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
        }

        TransactionSynchronizationManager.BindResource(connectionFactory, resourceHolder);
        resourceHolder.SynchronizedWithTransaction = true;
        if (TransactionSynchronizationManager.IsSynchronizationActive())
        {
            TransactionSynchronizationManager.RegisterSynchronization(new RabbitResourceSynchronization(resourceHolder, connectionFactory));
        }

        return resourceHolder;
    }

    public static IConnection CreateConnection(IConnectionFactory connectionFactory, bool publisherConnectionIfPossible)
    {
        if (publisherConnectionIfPossible)
        {
            var publisherFactory = connectionFactory.PublisherConnectionFactory;
            if (publisherFactory != null)
            {
                return publisherFactory.CreateConnection();
            }
        }

        return connectionFactory.CreateConnection();
    }

    public static void RegisterDeliveryTag(IConnectionFactory connectionFactory, RC.IModel channel, ulong tag)
    {
        if (connectionFactory == null)
        {
            throw new ArgumentNullException(nameof(connectionFactory));
        }

        var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
        if (resourceHolder != null)
        {
            resourceHolder.AddDeliveryTag(channel, tag);
        }
    }

    private static RabbitResourceHolder DoGetTransactionalResourceHolder(IConnectionFactory connectionFactory, IResourceFactory resourceFactory)
    {
        if (connectionFactory == null)
        {
            throw new ArgumentNullException(nameof(connectionFactory));
        }

        if (resourceFactory == null)
        {
            throw new ArgumentNullException(nameof(resourceFactory));
        }

        var resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
        if (resourceHolder != null)
        {
            var model = resourceFactory.GetChannel(resourceHolder);
            if (model != null)
            {
                return resourceHolder;
            }
        }

        var resourceHolderToUse = resourceHolder ?? new RabbitResourceHolder();

        var connection = resourceFactory.GetConnection(resourceHolderToUse);
        RC.IModel channel;
        try
        {
            /*
             * If we are in a listener container, first see if there's a channel registered
             * for this consumer and the consumer is using the same connection factory.
             */
            channel = ConsumerChannelRegistry.GetConsumerChannel(connectionFactory);
            if (channel == null && connection == null)
            {
                connection = resourceFactory.CreateConnection2();
                if (resourceHolder == null)
                {
                    /*
                     * While creating a connection, a connection listener might have created a
                     * transactional channel and bound it to the transaction.
                     */
                    resourceHolder = (RabbitResourceHolder)TransactionSynchronizationManager.GetResource(connectionFactory);
                    if (resourceHolder != null)
                    {
                        channel = resourceHolder.GetChannel();
                        resourceHolderToUse = resourceHolder;
                    }
                }

                resourceHolderToUse.AddConnection(connection);
            }

            channel ??= resourceFactory.CreateChannel(connection);

            resourceHolderToUse.AddChannel(channel, connection);

            if (!resourceHolderToUse.Equals(resourceHolder))
            {
                BindResourceToTransaction(resourceHolderToUse, connectionFactory, resourceFactory.IsSynchedLocalTransactionAllowed);
            }

            return resourceHolderToUse;
        }
        catch (Exception ex)
        {
            RabbitUtils.CloseConnection(connection);
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(ex);
        }
    }

    public interface IResourceFactory
    {
        RC.IModel GetChannel(RabbitResourceHolder holder);

        IConnection GetConnection(RabbitResourceHolder holder);

        IConnection CreateConnection2();

        RC.IModel CreateChannel(IConnection connection);

        bool IsSynchedLocalTransactionAllowed { get; }
    }

    private sealed class RabbitResourceFactory : IResourceFactory
    {
        public IConnectionFactory ConnectionFactory { get; }

        public bool IsSynchedLocalTransactionAllowed { get; }

        public bool PublisherConnectionIfPossible { get; }

        public RabbitResourceFactory(IConnectionFactory connectionFactory, bool synchedLocalTransactionAllowed, bool publisherConnectionIfPossible)
        {
            ConnectionFactory = connectionFactory;
            IsSynchedLocalTransactionAllowed = synchedLocalTransactionAllowed;
            PublisherConnectionIfPossible = publisherConnectionIfPossible;
        }

        public RC.IModel CreateChannel(IConnection connection)
        {
            return connection.CreateChannel(IsSynchedLocalTransactionAllowed);
        }

        public IConnection CreateConnection2()
        {
            return CreateConnection(ConnectionFactory, PublisherConnectionIfPossible);
        }

        public RC.IModel GetChannel(RabbitResourceHolder holder)
        {
            return holder.GetChannel();
        }

        public IConnection GetConnection(RabbitResourceHolder holder)
        {
            return holder.GetConnection();
        }
    }

    private sealed class RabbitResourceSynchronization : ResourceHolderSynchronization<RabbitResourceHolder, object>
    {
        private readonly RabbitResourceHolder _resourceHolder;

        public RabbitResourceSynchronization(RabbitResourceHolder resourceHolder, object resourceKey)
            : base(resourceHolder, resourceKey)
        {
            _resourceHolder = resourceHolder;
        }

        public override void AfterCompletion(int status)
        {
            if (status == AbstractTransactionSynchronization.STATUS_COMMITTED)
            {
                _resourceHolder.CommitAll();
            }
            else
            {
                _resourceHolder.RollbackAll();
            }

            if (_resourceHolder.ReleaseAfterCompletion)
            {
                _resourceHolder.SynchronizedWithTransaction = false;
            }

            base.AfterCompletion(status);
        }

        protected override bool ShouldReleaseBeforeCompletion()
        {
            return false;
        }

        protected override void ReleaseResource(RabbitResourceHolder resourceHolder, object resourceKey)
        {
            ReleaseResources(resourceHolder);
        }
    }
}
