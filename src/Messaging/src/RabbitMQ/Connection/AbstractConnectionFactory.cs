// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Net;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Steeltoe.Messaging.Rabbit.Connection
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public abstract class AbstractConnectionFactory : IConnectionFactory
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public const int DEFAULT_CLOSE_TIMEOUT = 30000;
        protected readonly ILogger _logger;
        protected readonly RabbitMQ.Client.IConnectionFactory _rabbitConnectionFactory;

        private const string PUBLISHER_SUFFIX = ".publisher";
        private readonly CompositeConnectionListener _connectionListener = new CompositeConnectionListener();
        private readonly CompositeChannelListener _channelListener = new CompositeChannelListener();
        private readonly Random _random = new Random();
        private int _defaultConnectionNameStrategyCounter;

        protected AbstractConnectionFactory(
            RabbitMQ.Client.IConnectionFactory rabbitConnectionFactory,
            ILogger logger = null)
            : this(rabbitConnectionFactory, null, logger)
        {
        }

        protected AbstractConnectionFactory(
            RabbitMQ.Client.IConnectionFactory rabbitConnectionFactory,
            AbstractConnectionFactory publisherConnectionFactory,
            ILogger logger = null)
        {
            if (rabbitConnectionFactory == null)
            {
                throw new ArgumentNullException(nameof(rabbitConnectionFactory));
            }

            _rabbitConnectionFactory = rabbitConnectionFactory;
            PublisherConnectionFactory = publisherConnectionFactory;
            _logger = logger;
            RecoveryListener = new DefaultRecoveryListener(logger);
            BlockedListener = new DefaultBlockedListener(logger);
            Name = GetType().Name + "@" + GetHashCode();
        }

        public virtual RabbitMQ.Client.ConnectionFactory RabbitConnectionFactory => _rabbitConnectionFactory as RabbitMQ.Client.ConnectionFactory;

        public virtual string Username
        {
            get { return _rabbitConnectionFactory.UserName; }
            set { _rabbitConnectionFactory.UserName = value; }
        }

        public virtual string Password
        {
            get { return _rabbitConnectionFactory.Password; }
            set { _rabbitConnectionFactory.Password = value; }
        }

        public virtual Uri Uri
        {
            get { return _rabbitConnectionFactory.Uri; }
            set { _rabbitConnectionFactory.Uri = value; }
        }

        public virtual string Host
        {
            get { return (RabbitConnectionFactory == null) ? "localhost" : RabbitConnectionFactory.HostName; }
            set { RabbitConnectionFactory.HostName = value; }
        }

        public virtual int Port
        {
            get { return (RabbitConnectionFactory == null) ? -1 : RabbitConnectionFactory.Port; }
            set { RabbitConnectionFactory.Port = value; }
        }

        public virtual string VirtualHost
        {
            get { return _rabbitConnectionFactory.VirtualHost; }
            set { _rabbitConnectionFactory.VirtualHost = value; }
        }

        public virtual ushort RequestedHeartBeat
        {
            get { return _rabbitConnectionFactory.RequestedHeartbeat; }
            set { _rabbitConnectionFactory.RequestedHeartbeat = value; }
        }

        public virtual int ConnectionTimeout
        {
            get { return (RabbitConnectionFactory == null) ? 30000 : RabbitConnectionFactory.RequestedConnectionTimeout; }
            set { RabbitConnectionFactory.RequestedConnectionTimeout = value; }
        }

        public virtual int CloseTimeout { get; set; } = DEFAULT_CLOSE_TIMEOUT;

        public virtual string Name { get; set; }

        public virtual bool ShuffleAddresses { get; set; }

        public virtual List<AmqpTcpEndpoint> Addresses { get; set; }

        public virtual bool HasPublisherConnectionFactory => PublisherConnectionFactory != null;

        public virtual IConnectionFactory PublisherConnectionFactory { get; protected set; }

        public virtual IBlockedListener BlockedListener { get; set; }

        public virtual IRecoveryListener RecoveryListener { get; set; }

        public virtual bool IsSimplePublisherConfirms { get; set; } = false;

        public virtual bool IsPublisherConfirms { get; set; } = false;

        public virtual bool IsPublisherReturns { get; set; } = false;

        public virtual IConnectionListener ConnectionListener => _connectionListener;

        public virtual IChannelListener ChannelListener => _channelListener;

        public virtual void SetConnectionListeners(List<IConnectionListener> listeners)
        {
            _connectionListener.SetListeners(listeners);
            if (PublisherConnectionFactory != null)
            {
                AbstractPublisherConnectionFactory.SetConnectionListeners(listeners);
            }
        }

        public virtual void AddConnectionListener(IConnectionListener connectionListener)
        {
            _connectionListener.AddListener(connectionListener);
            if (PublisherConnectionFactory != null)
            {
                PublisherConnectionFactory.AddConnectionListener(connectionListener);
            }
        }

        public virtual bool RemoveConnectionListener(IConnectionListener connectionListener)
        {
            var result = _connectionListener.RemoveListener(connectionListener);
            if (PublisherConnectionFactory != null)
            {
                PublisherConnectionFactory.RemoveConnectionListener(connectionListener);
            }

            return result;
        }

        public virtual void ClearConnectionListeners()
        {
            _connectionListener.ClearListeners();
            if (PublisherConnectionFactory != null)
            {
                PublisherConnectionFactory.ClearConnectionListeners();
            }
        }

        public virtual void SetChannelListeners(List<IChannelListener> listeners)
        {
            _channelListener.SetListeners(listeners);
        }

        public virtual void AddChannelListener(IChannelListener listener)
        {
            _channelListener.AddListener(listener);
            if (PublisherConnectionFactory != null)
            {
                AbstractPublisherConnectionFactory.AddChannelListener(listener);
            }
        }

        public virtual void SetRecoveryListener(IRecoveryListener recoveryListener)
        {
            RecoveryListener = recoveryListener;
            if (PublisherConnectionFactory != null)
            {
                AbstractPublisherConnectionFactory.SetRecoveryListener(recoveryListener);
            }
        }

        public virtual void SetBlockedListener(IBlockedListener blockedListener)
        {
            BlockedListener = blockedListener;
            if (PublisherConnectionFactory != null)
            {
                AbstractPublisherConnectionFactory.SetBlockedListener(blockedListener);
            }
        }

        public virtual void SetAddresses(string addresses)
        {
            if (!string.IsNullOrEmpty(addresses))
            {
                var endpoints = AmqpTcpEndpoint.ParseMultiple(addresses);
                if (endpoints.Length > 0)
                {
                    Addresses = endpoints.ToList();
                    if (PublisherConnectionFactory != null)
                    {
                        AbstractPublisherConnectionFactory.SetAddresses(addresses);
                    }

                    return;
                }
            }

            _logger?.LogInformation("SetAddresses() called with an empty value, will be using the host+port properties for connections");
            Addresses = null;
        }

        public abstract IConnection CreateConnection();

        public virtual void Destroy()
        {
            if (PublisherConnectionFactory != null)
            {
                PublisherConnectionFactory.Destroy();
            }
        }

        public virtual void Dispose()
        {
            Destroy();
        }

        public override string ToString()
        {
            return Name;
        }

        protected AbstractConnectionFactory AbstractPublisherConnectionFactory
        {
            get
            {
                return (AbstractConnectionFactory)PublisherConnectionFactory;
            }
        }

        protected virtual IConnection CreateBareConnection()
        {
            try
            {
                var connectionName = ObtainNewConnectionName();

                var rabbitConnection = Connect(connectionName);

                var connection = new SimpleConnection(rabbitConnection, CloseTimeout, _logger);

                _logger?.LogInformation("Created new connection: " + connectionName + "/" + connection);

                if (rabbitConnection != null && RecoveryListener != null)
                {
                    rabbitConnection.RecoverySucceeded += RecoveryListener.HandleRecoverySucceeded;
                    rabbitConnection.ConnectionRecoveryError += RecoveryListener.HandleConnectionRecoveryError;
                }

                if (rabbitConnection != null && BlockedListener != null)
                {
                    rabbitConnection.ConnectionBlocked += BlockedListener.HandleBlocked;
                    rabbitConnection.ConnectionUnblocked += BlockedListener.HandleUnblocked;
                }

                return connection;
            }
            catch (Exception e) when (e is IOException || e is TimeoutException)
            {
                throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
            }
        }

        protected virtual string GetDefaultHostName()
        {
            string temp;
            try
            {
                var inetUtils = new InetUtils(new InetOptions(), _logger);
                var hostInfo = inetUtils.FindFirstNonLoopbackHostInfo();
                temp = hostInfo.Hostname;
                _logger?.LogDebug("Using hostname [" + temp + "] for hostname.");
            }
            catch (Exception e)
            {
                _logger?.LogWarning("Could not get host name, using 'localhost' as default value", e);
                temp = "localhost";
            }

            return temp;
        }

        protected virtual string ObtainNewConnectionName()
        {
            return Name + ":" + Interlocked.Increment(ref _defaultConnectionNameStrategyCounter) + PUBLISHER_SUFFIX;
        }

        private RabbitMQ.Client.IConnection Connect(string connectionName)
        {
            RabbitMQ.Client.IConnection rabbitConnection;
            if (Addresses != null)
            {
                var addressesToConnect = Addresses;
                if (ShuffleAddresses && addressesToConnect.Count > 1)
                {
                    var list = addressesToConnect.ToArray();
                    Shuffle(list);
                    addressesToConnect = list.ToList();
                }

                _logger?.LogInformation("Attempting to connect to: " + addressesToConnect);

                rabbitConnection = _rabbitConnectionFactory.CreateConnection(addressesToConnect, connectionName);
            }
            else
            {
                _logger?.LogInformation("Attempting to connect to: " + Host + ":" + Port);
                rabbitConnection = _rabbitConnectionFactory.CreateConnection(connectionName);
            }

            return rabbitConnection;
        }

        private void Shuffle<T>(T[] array)
        {
            var n = array.Length;
            for (var i = 0; i < (n - 1); i++)
            {
                var r = i + _random.Next(n - i);
                var t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
        }

        private class DefaultBlockedListener : IBlockedListener
        {
            private readonly ILogger _logger;

            public DefaultBlockedListener(ILogger logger)
            {
                _logger = logger;
            }

            public void HandleBlocked(object sender, ConnectionBlockedEventArgs args)
            {
                _logger?.LogInformation("Connection blocked: {reason}", args.Reason);
            }

            public void HandleUnblocked(object sender, EventArgs args)
            {
                _logger?.LogInformation("Connection unblocked");
            }
        }

        private class DefaultRecoveryListener : IRecoveryListener
        {
            private readonly ILogger _logger;

            public DefaultRecoveryListener(ILogger logger)
            {
                _logger = logger;
            }

            public void HandleConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs error)
            {
                _logger?.LogDebug("Connection recovery failed: " + error.Exception);
            }

            public void HandleRecoverySucceeded(object sender, EventArgs e)
            {
                _logger?.LogDebug("Connection recovery succeed");
            }
        }
    }
}
