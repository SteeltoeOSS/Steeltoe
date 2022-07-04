// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Net;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public abstract class AbstractConnectionFactory : IConnectionFactory
{
    public const int DefaultCloseTimeout = 30000;
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly ILogger Logger;
    protected readonly RC.IConnectionFactory InnerRabbitConnectionFactory;

    private const string PublisherSuffix = ".publisher";
    private readonly CompositeConnectionListener _connectionListener;
    private readonly CompositeChannelListener _channelListener;
    private readonly Random _random = new ();
    private int _defaultConnectionNameStrategyCounter;

    protected AbstractConnectionFactory(
        RC.IConnectionFactory rabbitConnectionFactory,
        ILoggerFactory loggerFactory = null)
        : this(rabbitConnectionFactory, null, loggerFactory)
    {
    }

    protected AbstractConnectionFactory(
        RC.IConnectionFactory rabbitConnectionFactory,
        AbstractConnectionFactory publisherConnectionFactory,
        ILoggerFactory loggerFactory = null)
    {
        LoggerFactory = loggerFactory;
        Logger = LoggerFactory?.CreateLogger(GetType());
        InnerRabbitConnectionFactory = rabbitConnectionFactory ?? throw new ArgumentNullException(nameof(rabbitConnectionFactory));
        _connectionListener = new CompositeConnectionListener(LoggerFactory?.CreateLogger<CompositeConnectionListener>());
        _channelListener = new CompositeChannelListener(LoggerFactory?.CreateLogger<CompositeConnectionListener>());
        PublisherConnectionFactory = publisherConnectionFactory;
        RecoveryListener = new DefaultRecoveryListener(LoggerFactory?.CreateLogger<DefaultRecoveryListener>());
        BlockedListener = new DefaultBlockedListener(LoggerFactory?.CreateLogger<DefaultBlockedListener>());
        ServiceName = $"{GetType().Name}@{GetHashCode()}";
    }

    public virtual RC.ConnectionFactory RabbitConnectionFactory => InnerRabbitConnectionFactory as RC.ConnectionFactory;

    public virtual string Username
    {
        get { return InnerRabbitConnectionFactory.UserName; }
        set { InnerRabbitConnectionFactory.UserName = value; }
    }

    public virtual string Password
    {
        get { return InnerRabbitConnectionFactory.Password; }
        set { InnerRabbitConnectionFactory.Password = value; }
    }

    public virtual Uri Uri
    {
        get { return InnerRabbitConnectionFactory.Uri; }
        set { InnerRabbitConnectionFactory.Uri = value; }
    }

    public virtual string Host
    {
        get { return RabbitConnectionFactory == null ? "localhost" : RabbitConnectionFactory.HostName; }
        set { RabbitConnectionFactory.HostName = value; }
    }

    public virtual int Port
    {
        get { return RabbitConnectionFactory?.Port ?? -1; }
        set { RabbitConnectionFactory.Port = value; }
    }

    public virtual string VirtualHost
    {
        get { return InnerRabbitConnectionFactory.VirtualHost; }
        set { InnerRabbitConnectionFactory.VirtualHost = value; }
    }

    public virtual ushort RequestedHeartBeat
    {
        get { return InnerRabbitConnectionFactory.RequestedHeartbeat; }
        set { InnerRabbitConnectionFactory.RequestedHeartbeat = value; }
    }

    public virtual int ConnectionTimeout
    {
        get { return RabbitConnectionFactory?.RequestedConnectionTimeout ?? 30000; }
        set { RabbitConnectionFactory.RequestedConnectionTimeout = value; }
    }

    public virtual int CloseTimeout { get; set; } = DefaultCloseTimeout;

    public virtual string ServiceName { get; set; }

    public virtual bool ShuffleAddresses { get; set; }

    public virtual List<RC.AmqpTcpEndpoint> Addresses { get; set; }

    public virtual bool HasPublisherConnectionFactory => PublisherConnectionFactory != null;

    public virtual IConnectionFactory PublisherConnectionFactory { get; protected set; }

    public virtual IBlockedListener BlockedListener { get; set; }

    public virtual IRecoveryListener RecoveryListener { get; set; }

    public virtual bool IsSimplePublisherConfirms { get; set; }

    public virtual bool IsPublisherConfirms { get; set; }

    public virtual bool IsPublisherReturns { get; set; }

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
            var endpoints = RC.AmqpTcpEndpoint.ParseMultiple(addresses);
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

        Logger?.LogInformation("SetAddresses() called with an empty value, will be using the host+port properties for connections");
        Addresses = null;
    }

    public abstract IConnection CreateConnection();

    public virtual void Destroy()
    {
        PublisherConnectionFactory?.Destroy();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Destroy();
        }
    }

    public override string ToString()
    {
        return ServiceName;
    }

    protected internal virtual void ConnectionShutdownCompleted(object sender, RC.ShutdownEventArgs args)
    {
        ConnectionListener.OnShutDown(args);
    }

    protected virtual AbstractConnectionFactory AbstractPublisherConnectionFactory
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

            var connection = new SimpleConnection(rabbitConnection, CloseTimeout, LoggerFactory?.CreateLogger<SimpleConnection>());

            Logger?.LogInformation("Created new connection: {connectionName}/{connection}", connectionName, connection);

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

            if (rabbitConnection != null)
            {
                rabbitConnection.ConnectionShutdown += ConnectionShutdownCompleted;
            }

            return connection;
        }
        catch (Exception e)
        {
            throw RabbitExceptionTranslator.ConvertRabbitAccessException(e);
        }
    }

    protected virtual string GetDefaultHostName()
    {
        string temp;
        try
        {
            var inetUtils = new InetUtils(new InetOptions(), Logger);
            var hostInfo = inetUtils.FindFirstNonLoopbackHostInfo();
            temp = hostInfo.Hostname;
            Logger?.LogDebug("Using hostname [{name}] for hostname.", temp);
        }
        catch (Exception e)
        {
            Logger?.LogWarning("Could not get host name, using 'localhost' as default value", e);
            temp = "localhost";
        }

        return temp;
    }

    protected virtual string ObtainNewConnectionName()
    {
        return $"{ServiceName}:{Interlocked.Increment(ref _defaultConnectionNameStrategyCounter)}{PublisherSuffix}";
    }

    private RC.IConnection Connect(string connectionName)
    {
        RC.IConnection rabbitConnection;
        if (Addresses != null)
        {
            var addressesToConnect = Addresses;
            if (ShuffleAddresses && addressesToConnect.Count > 1)
            {
                var list = addressesToConnect.ToArray();
                Shuffle(list);
                addressesToConnect = list.ToList();
            }

            Logger?.LogInformation("Attempting to connect to: {address} ", addressesToConnect);

            rabbitConnection = InnerRabbitConnectionFactory.CreateConnection(addressesToConnect);
        }
        else
        {
            Logger?.LogInformation("Attempting to connect to: {host}:{port}", Host, Port);
            rabbitConnection = InnerRabbitConnectionFactory.CreateConnection(connectionName);
        }

        return rabbitConnection;
    }

    private void Shuffle<T>(T[] array)
    {
        var n = array.Length;
        for (var i = 0; i < n - 1; i++)
        {
            var r = i + _random.Next(n - i);
            (array[r], array[i]) = (array[i], array[r]);
        }
    }

    private sealed class DefaultBlockedListener : IBlockedListener
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
            _logger?.LogInformation("Connection unblocked: {args}", args.ToString());
        }
    }

    private sealed class DefaultRecoveryListener : IRecoveryListener
    {
        private readonly ILogger _logger;

        public DefaultRecoveryListener(ILogger logger)
        {
            _logger = logger;
        }

        public void HandleConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs error)
        {
            _logger?.LogDebug(error.Exception, "Connection recovery failed");
        }

        public void HandleRecoverySucceeded(object sender, EventArgs e)
        {
            _logger?.LogDebug("Connection recovery succeed");
        }
    }
}
