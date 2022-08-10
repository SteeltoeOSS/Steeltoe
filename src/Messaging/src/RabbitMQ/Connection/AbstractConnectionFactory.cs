// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Steeltoe.Common;
using Steeltoe.Common.Net;
using Steeltoe.Messaging.RabbitMQ.Support;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public abstract class AbstractConnectionFactory : IConnectionFactory
{
    private const string PublisherSuffix = ".publisher";
    public const int DefaultCloseTimeout = 30000;
    private readonly CompositeConnectionListener _connectionListener;
    private readonly CompositeChannelListener _channelListener;
    private readonly Random _random = new();
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly ILogger Logger;
    protected readonly RC.IConnectionFactory InnerRabbitConnectionFactory;
    private int _defaultConnectionNameStrategyCounter;

    protected virtual AbstractConnectionFactory AbstractPublisherConnectionFactory => (AbstractConnectionFactory)PublisherConnectionFactory;

    public virtual RC.ConnectionFactory RabbitConnectionFactory => InnerRabbitConnectionFactory as RC.ConnectionFactory;

    public virtual string Username
    {
        get => InnerRabbitConnectionFactory.UserName;
        set => InnerRabbitConnectionFactory.UserName = value;
    }

    public virtual string Password
    {
        get => InnerRabbitConnectionFactory.Password;
        set => InnerRabbitConnectionFactory.Password = value;
    }

    public virtual Uri Uri
    {
        get => InnerRabbitConnectionFactory.Uri;
        set => InnerRabbitConnectionFactory.Uri = value;
    }

    public virtual string Host
    {
        get => RabbitConnectionFactory == null ? "localhost" : RabbitConnectionFactory.HostName;
        set => RabbitConnectionFactory.HostName = value;
    }

    public virtual int Port
    {
        get => RabbitConnectionFactory?.Port ?? -1;
        set => RabbitConnectionFactory.Port = value;
    }

    public virtual string VirtualHost
    {
        get => InnerRabbitConnectionFactory.VirtualHost;
        set => InnerRabbitConnectionFactory.VirtualHost = value;
    }

    public virtual ushort RequestedHeartBeat
    {
        get => InnerRabbitConnectionFactory.RequestedHeartbeat;
        set => InnerRabbitConnectionFactory.RequestedHeartbeat = value;
    }

    public virtual int ConnectionTimeout
    {
        get => RabbitConnectionFactory?.RequestedConnectionTimeout ?? 30000;
        set => RabbitConnectionFactory.RequestedConnectionTimeout = value;
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

    protected AbstractConnectionFactory(RC.IConnectionFactory rabbitConnectionFactory, ILoggerFactory loggerFactory = null)
        : this(rabbitConnectionFactory, null, loggerFactory)
    {
    }

    protected AbstractConnectionFactory(RC.IConnectionFactory rabbitConnectionFactory, AbstractConnectionFactory publisherConnectionFactory,
        ILoggerFactory loggerFactory = null)
    {
        ArgumentGuard.NotNull(rabbitConnectionFactory);

        LoggerFactory = loggerFactory;
        Logger = LoggerFactory?.CreateLogger(GetType());
        InnerRabbitConnectionFactory = rabbitConnectionFactory;
        _connectionListener = new CompositeConnectionListener(LoggerFactory?.CreateLogger<CompositeConnectionListener>());
        _channelListener = new CompositeChannelListener(LoggerFactory?.CreateLogger<CompositeConnectionListener>());
        PublisherConnectionFactory = publisherConnectionFactory;
        RecoveryListener = new DefaultRecoveryListener(LoggerFactory?.CreateLogger<DefaultRecoveryListener>());
        BlockedListener = new DefaultBlockedListener(LoggerFactory?.CreateLogger<DefaultBlockedListener>());
        ServiceName = $"{GetType().Name}@{GetHashCode()}";
    }

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
        bool result = _connectionListener.RemoveListener(connectionListener);

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
            RC.AmqpTcpEndpoint[] endpoints = RC.AmqpTcpEndpoint.ParseMultiple(addresses);

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

    protected virtual IConnection CreateBareConnection()
    {
        try
        {
            string connectionName = ObtainNewConnectionName();

            RC.IConnection rabbitConnection = Connect(connectionName);

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
            HostInfo hostInfo = inetUtils.FindFirstNonLoopbackHostInfo();
            temp = hostInfo.Hostname;
            Logger?.LogDebug("Using hostname [{name}] for hostname.", temp);
        }
        catch (Exception e)
        {
            Logger?.LogWarning(e, "Could not get host name, using 'localhost' as default value");
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
            List<RC.AmqpTcpEndpoint> addressesToConnect = Addresses;

            if (ShuffleAddresses && addressesToConnect.Count > 1)
            {
                RC.AmqpTcpEndpoint[] list = addressesToConnect.ToArray();
                Shuffle(list);
                addressesToConnect = list.ToList();
            }

            Logger?.LogInformation("Attempting to connect to: {address}", addressesToConnect);

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
        int n = array.Length;

        for (int i = 0; i < n - 1; i++)
        {
            int r = i + _random.Next(n - i);
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
            _logger?.LogInformation("Connection unblocked: {args}", args);
        }
    }

    private sealed class DefaultRecoveryListener : IRecoveryListener
    {
        private readonly ILogger _logger;

        public DefaultRecoveryListener(ILogger logger)
        {
            _logger = logger;
        }

        public void HandleConnectionRecoveryError(object sender, ConnectionRecoveryErrorEventArgs args)
        {
            _logger?.LogDebug(args.Exception, "Connection recovery failed");
        }

        public void HandleRecoverySucceeded(object sender, EventArgs args)
        {
            _logger?.LogDebug("Connection recovery succeed");
        }
    }
}
