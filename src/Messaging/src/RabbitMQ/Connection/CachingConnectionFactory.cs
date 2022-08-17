// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class CachingConnectionFactory : AbstractConnectionFactory, IShutdownListener
{
    private const int DefaultChannelCacheSize = 25;
    public const string DefaultServiceName = "ccFactory";

    private readonly object _connectionMonitor = new();
    private readonly Dictionary<int, AtomicInteger> _channelHighWaterMarks = new();
    private readonly AtomicInteger _connectionHighWaterMark = new();
    private readonly IOptionsMonitor<RabbitOptions> _optionsMonitor;

    // Internal for unit tests
    internal readonly Dictionary<IConnection, SemaphoreSlim> CheckoutPermits = new();
    internal readonly LinkedList<IChannelProxy> CachedChannelsNonTransactional = new();
    internal readonly LinkedList<IChannelProxy> CachedChannelsTransactional = new();
    internal readonly HashSet<ChannelCachingConnectionProxy> AllocatedConnections = new();
    internal readonly LinkedList<ChannelCachingConnectionProxy> IdleConnections = new();
    internal readonly Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> AllocatedConnectionNonTransactionalChannels = new();
    internal readonly Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> AllocatedConnectionTransactionalChannels = new();
    internal readonly ChannelCachingConnectionProxy Connection;

    private int _channelCacheSize = DefaultChannelCacheSize;
    private int _connectionCacheSize = 1;
    private int _connectionLimit = int.MaxValue;
    private bool _publisherReturns;
    private ConfirmType _confirmType = ConfirmType.None;
    private int _channelCheckoutTimeout;
    private IConditionalExceptionLogger _closeExceptionLogger = new DefaultChannelCloseLogger();
    private bool _active = true;
    internal bool Stopped;

    protected internal RabbitOptions Options
    {
        get
        {
            if (_optionsMonitor != null)
            {
                return _optionsMonitor.CurrentValue;
            }

            return null;
        }
    }

    public CachingConnectionFactory PublisherCachingConnectionFactory
    {
        get => (CachingConnectionFactory)PublisherConnectionFactory;
        set => PublisherConnectionFactory = value;
    }

    public int ChannelCacheSize
    {
        get => _channelCacheSize;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be zero or negative.");
            }

            _channelCacheSize = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.ChannelCacheSize = value;
            }
        }
    }

    public CachingMode CacheMode { get; set; } = CachingMode.Channel;

    public int ConnectionCacheSize
    {
        get => _connectionCacheSize;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be zero or negative.");
            }

            _connectionCacheSize = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.ConnectionCacheSize = value;
            }
        }
    }

    public int ConnectionLimit
    {
        get => _connectionLimit;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value cannot be zero or negative.");
            }

            _connectionLimit = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.ConnectionLimit = value;
            }
        }
    }

    public override bool IsPublisherReturns
    {
        get => _publisherReturns;
        set
        {
            _publisherReturns = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.IsPublisherReturns = value;
            }
        }
    }

    public ConfirmType PublisherConfirmType
    {
        get => _confirmType;
        set
        {
            _confirmType = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.PublisherConfirmType = value;
            }
        }
    }

    public override bool IsPublisherConfirms => _confirmType == ConfirmType.Correlated;

    public override bool IsSimplePublisherConfirms => _confirmType == ConfirmType.Simple;

    public int ChannelCheckoutTimeout
    {
        get => _channelCheckoutTimeout;
        set
        {
            _channelCheckoutTimeout = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.ChannelCheckoutTimeout = value;
            }
        }
    }

    public IConditionalExceptionLogger CloseExceptionLogger
    {
        get => _closeExceptionLogger;
        set
        {
            _closeExceptionLogger = value;

            if (PublisherConnectionFactory != null)
            {
                PublisherCachingConnectionFactory.CloseExceptionLogger = value;
            }
        }
    }

    public IPublisherCallbackChannelFactory PublisherCallbackChannelFactory { get; set; }

    public CachingConnectionFactory(ILoggerFactory loggerFactory = null)
        : this(null, -1, loggerFactory)
    {
    }

    [ActivatorUtilitiesConstructor]
    public CachingConnectionFactory(IOptionsMonitor<RabbitOptions> optionsMonitor, ILoggerFactory loggerFactory = null)
        : base(NewRabbitConnectionFactory(), loggerFactory)
    {
        _optionsMonitor = optionsMonitor;
        Connection = new ChannelCachingConnectionProxy(this, null, loggerFactory?.CreateLogger<ChannelCachingConnectionProxy>());
        ConfigureRabbitConnectionFactory(Options);
        PublisherConnectionFactory = new CachingConnectionFactory(InnerRabbitConnectionFactory, true, CachingMode.Channel, loggerFactory);
        PublisherCallbackChannelFactory = new DefaultPublisherCallbackFactory(loggerFactory);
        Configure(Options);
        InitCacheWaterMarks();
        ServiceName = DefaultServiceName;
    }

    public CachingConnectionFactory(string hostname, ILoggerFactory loggerFactory = null)
        : this(hostname, -1, loggerFactory)
    {
    }

    public CachingConnectionFactory(int port, ILoggerFactory loggerFactory = null)
        : this(null, port, loggerFactory)
    {
    }

    public CachingConnectionFactory(string hostNameArg, int port, ILoggerFactory loggerFactory = null)
        : base(NewRabbitConnectionFactory(), loggerFactory)
    {
        string hostname = hostNameArg;

        if (string.IsNullOrEmpty(hostname))
        {
            hostname = GetDefaultHostName();
        }

        Connection = new ChannelCachingConnectionProxy(this, null, loggerFactory?.CreateLogger<ChannelCachingConnectionProxy>());
        Host = hostname;
        Port = port;
        PublisherConnectionFactory = new CachingConnectionFactory(InnerRabbitConnectionFactory, true, CachingMode.Channel, loggerFactory);
        PublisherCallbackChannelFactory = new DefaultPublisherCallbackFactory(loggerFactory);
        InitCacheWaterMarks();
        ServiceName = DefaultServiceName;
    }

    public CachingConnectionFactory(string hostNameArg, int port, RC.IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
        : base(connectionFactory, loggerFactory)
    {
        string hostname = hostNameArg;

        if (string.IsNullOrEmpty(hostname))
        {
            hostname = GetDefaultHostName();
        }

        Connection = new ChannelCachingConnectionProxy(this, null, loggerFactory?.CreateLogger<ChannelCachingConnectionProxy>());
        Host = hostname;
        Port = port;
        PublisherConnectionFactory = new CachingConnectionFactory(InnerRabbitConnectionFactory, true, CachingMode.Channel, loggerFactory);
        PublisherCallbackChannelFactory = new DefaultPublisherCallbackFactory(loggerFactory);
        InitCacheWaterMarks();
        ServiceName = DefaultServiceName;
    }

    public CachingConnectionFactory(Uri uri, ILoggerFactory loggerFactory = null)
        : this(uri, CachingMode.Channel, loggerFactory)
    {
    }

    public CachingConnectionFactory(Uri uri, CachingMode cachingMode = CachingMode.Channel, ILoggerFactory loggerFactory = null)
        : base(NewRabbitConnectionFactory(), loggerFactory)
    {
        Connection = new ChannelCachingConnectionProxy(this, null, loggerFactory?.CreateLogger<ChannelCachingConnectionProxy>());
        CacheMode = cachingMode;
        Uri = uri;
        PublisherConnectionFactory = new CachingConnectionFactory(InnerRabbitConnectionFactory, true, cachingMode, loggerFactory);
        PublisherCallbackChannelFactory = new DefaultPublisherCallbackFactory(loggerFactory);
        InitCacheWaterMarks();
        ServiceName = DefaultServiceName;
    }

    protected internal CachingConnectionFactory(RC.IConnectionFactory rabbitConnectionFactory, ILoggerFactory loggerFactory = null)
        : this(rabbitConnectionFactory, false, CachingMode.Channel, loggerFactory)
    {
    }

    protected internal CachingConnectionFactory(RC.IConnectionFactory rabbitConnectionFactory, bool isPublisherFactory,
        CachingMode cachingMode = CachingMode.Channel, ILoggerFactory loggerFactory = null)
        : base(rabbitConnectionFactory, loggerFactory)
    {
        CacheMode = cachingMode;
        Connection = new ChannelCachingConnectionProxy(this, null, loggerFactory?.CreateLogger<ChannelCachingConnectionProxy>());
        PublisherCallbackChannelFactory = new DefaultPublisherCallbackFactory(loggerFactory);

        if (!isPublisherFactory)
        {
            if (RabbitConnectionFactory != null && (RabbitConnectionFactory.AutomaticRecoveryEnabled || RabbitConnectionFactory.TopologyRecoveryEnabled))
            {
                RabbitConnectionFactory.AutomaticRecoveryEnabled = false;
                RabbitConnectionFactory.TopologyRecoveryEnabled = false;

                Logger?.LogWarning("***\nAutomatic Recovery was Enabled in the provided connection factory;\n" +
                    "while Steeltoe is generally compatible with this feature, there\n" + "are some corner cases where problems arise. Steeltoe\n" +
                    "prefers to use its own recovery mechanisms; when this option is true, you may receive\n" +
                    "odd Exception's until the connection is recovered.\n");
            }

            PublisherConnectionFactory = new CachingConnectionFactory(InnerRabbitConnectionFactory, true, cachingMode, loggerFactory);
        }
        else
        {
            PublisherConnectionFactory = null;
        }

        InitCacheWaterMarks();
        ServiceName = DefaultServiceName;
    }

    private static RC.ConnectionFactory NewRabbitConnectionFactory()
    {
        var connectionFactory = new RC.ConnectionFactory
        {
            AutomaticRecoveryEnabled = false
        };

        return connectionFactory;
    }

    public override void SetConnectionListeners(List<IConnectionListener> listeners)
    {
        base.SetConnectionListeners(listeners);

        if (Connection.Target != null)
        {
            ConnectionListener.OnCreate(Connection);
        }
    }

    public override void AddConnectionListener(IConnectionListener connectionListener)
    {
        base.AddConnectionListener(connectionListener);

        if (Connection.Target != null)
        {
            connectionListener.OnCreate(Connection);
        }
    }

    public void ChannelShutdownCompleted(object sender, RC.ShutdownEventArgs args)
    {
        _closeExceptionLogger.Log(Logger, "Channel shutdown", args);
        ChannelListener.OnShutDown(args);
    }

    public override IConnection CreateConnection()
    {
        if (Stopped)
        {
            throw new RabbitApplicationContextClosedException("The ConnectionFactory is disposed and can no longer create connections.");
        }

        lock (_connectionMonitor)
        {
            if (CacheMode == CachingMode.Channel)
            {
                if (Connection.Target == null)
                {
                    Connection.Target = CreateBareConnection();

                    // invoke the listener *after* this.connection is assigned
                    if (!CheckoutPermits.ContainsKey(Connection))
                    {
                        CheckoutPermits.Add(Connection, new SemaphoreSlim(ChannelCacheSize));
                    }

                    Connection.CloseNotified = 0;
                    ConnectionListener.OnCreate(Connection);
                }

                return Connection;
            }

            if (CacheMode == CachingMode.Connection)
            {
                return GetConnectionFromCache();
            }
        }

        return null;
    }

    public override void Destroy()
    {
        base.Destroy();
        ResetConnection();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Destroy();
            Stopped = true;
        }

        base.Dispose(disposing);
    }

    public IDictionary<string, object> GetCacheProperties()
    {
        var props = new Dictionary<string, object>
        {
            { "cacheMode", CacheMode.ToString() }
        };

        lock (_connectionMonitor)
        {
            props.Add("channelCacheSize", _channelCacheSize);

            if (CacheMode == CachingMode.Connection)
            {
                props.Add("connectionCacheSize", _connectionCacheSize);
                props.Add("openConnections", CountOpenConnections());
                props.Add("idleConnections", IdleConnections.Count);
                props.Add("idleConnectionsHighWater", _connectionHighWaterMark.Value);

                foreach (ChannelCachingConnectionProxy proxy in AllocatedConnections)
                {
                    PutConnectionName(props, proxy, $":{proxy.LocalPort}");
                }

                foreach (KeyValuePair<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> entry in AllocatedConnectionTransactionalChannels)
                {
                    int port = entry.Key.LocalPort;

                    if (port > 0 && entry.Key.IsOpen)
                    {
                        LinkedList<IChannelProxy> channelList = entry.Value;
                        props.Add($"idleChannelsTx:{port}", channelList.Count);
                        props.Add($"idleChannelsTxHighWater:{port}", _channelHighWaterMarks[RuntimeHelpers.GetHashCode(channelList)].Value);
                    }
                }

                foreach (KeyValuePair<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> entry in AllocatedConnectionNonTransactionalChannels)
                {
                    int port = entry.Key.LocalPort;

                    if (port > 0 && entry.Key.IsOpen)
                    {
                        LinkedList<IChannelProxy> channelList = entry.Value;
                        props.Add($"idleChannelsNotTx:{port}", channelList.Count);
                        props.Add($"idleChannelsNotTxHighWater:{port}", _channelHighWaterMarks[RuntimeHelpers.GetHashCode(channelList)].Value);
                    }
                }
            }
            else
            {
                props.Add("localPort", Connection.Target == null ? 0 : Connection.LocalPort);
                props.Add("idleChannelsTx", CachedChannelsTransactional.Count);
                props.Add("idleChannelsNotTx", CachedChannelsNonTransactional.Count);
                props.Add("idleChannelsTxHighWater", _channelHighWaterMarks[RuntimeHelpers.GetHashCode(CachedChannelsTransactional)].Value);
                props.Add("idleChannelsNotTxHighWater", _channelHighWaterMarks[RuntimeHelpers.GetHashCode(CachedChannelsNonTransactional)].Value);
                PutConnectionName(props, Connection, string.Empty);
            }
        }

        return props;
    }

    public void ResetConnection()
    {
        lock (_connectionMonitor)
        {
            if (Connection.Target != null)
            {
                Connection.Dispose();
            }

            foreach (ChannelCachingConnectionProxy c in AllocatedConnections)
            {
                c.Dispose();
            }

            foreach (KeyValuePair<int, AtomicInteger> c in _channelHighWaterMarks)
            {
                c.Value.Value = 0;
            }

            _connectionHighWaterMark.Value = 0;
        }

        if (PublisherConnectionFactory != null)
        {
            PublisherCachingConnectionFactory.ResetConnection();
        }
    }

    // Used in unit test
    internal int CountOpenConnections()
    {
        return AllocatedConnections.Count(conn => conn.IsOpen);
    }

    protected void Reset(LinkedList<IChannelProxy> channels, LinkedList<IChannelProxy> txChannels, Dictionary<RC.IModel, IChannelProxy> channelsAwaitingAcks)
    {
        _active = false;
        CloseAndClear(channels);
        CloseAndClear(txChannels);
        CloseChannels(channelsAwaitingAcks.Values);
        channelsAwaitingAcks.Clear();
        _active = true;
    }

    protected void CloseAndClear(LinkedList<IChannelProxy> theChannels)
    {
        lock (theChannels)
        {
            CloseChannels(theChannels);
            theChannels.Clear();
        }
    }

    protected void CloseChannels(ICollection<IChannelProxy> theChannels)
    {
        foreach (IChannelProxy channel in theChannels)
        {
            try
            {
                channel.Close();
            }
            catch (Exception ex)
            {
                Logger?.LogTrace(ex, "Could not close cached Rabbit Channel");
            }
        }
    }

    private void Configure(RabbitOptions options)
    {
        SetAddresses(options.DetermineAddresses());
        IsPublisherConfirms = options.PublisherConfirms;
        IsPublisherReturns = options.PublisherReturns;
        RabbitOptions.ChannelOptions cacheChannel = options.Cache.Channel;

        if (cacheChannel.Size.HasValue)
        {
            ChannelCacheSize = cacheChannel.Size.Value;
        }

        if (cacheChannel.CheckoutTimeout.HasValue)
        {
            int asMilliseconds = (int)cacheChannel.CheckoutTimeout.Value.TotalMilliseconds;
            ChannelCheckoutTimeout = asMilliseconds;
        }

        RabbitOptions.ConnectionOptions cacheConnection = options.Cache.Connection;
        CacheMode = cacheConnection.Mode;

        if (cacheConnection.Size.HasValue)
        {
            ConnectionCacheSize = cacheConnection.Size.Value;
        }
    }

    private void PutConnectionName(IDictionary<string, object> props, IConnectionProxy connection, string keySuffix)
    {
        IConnection targetConnection = connection.TargetConnection;

        if (targetConnection != null)
        {
            RC.IConnection del = targetConnection.Connection;

            if (del != null)
            {
                string name = del.ClientProvidedName;

                if (name != null)
                {
                    props.Add($"connectionName{keySuffix}", name);
                }
            }
        }
    }

    private void ConfigureRabbitConnectionFactory(RabbitOptions options)
    {
        var factory = InnerRabbitConnectionFactory as RC.ConnectionFactory;
        string host = options.DetermineHost();

        if (host != null)
        {
            factory.HostName = host;
        }

        factory.Port = options.DeterminePort();
        string userName = options.DetermineUsername();

        if (userName != null)
        {
            factory.UserName = userName;
        }

        string password = options.DeterminePassword();

        if (password != null)
        {
            factory.Password = password;
        }

        string virtualHost = options.DetermineVirtualHost();

        if (virtualHost != null)
        {
            factory.VirtualHost = virtualHost;
        }

        if (options.RequestedHeartbeat.HasValue)
        {
            ushort asShortSeconds = (ushort)options.RequestedHeartbeat.Value.TotalSeconds;
            factory.RequestedHeartbeat = asShortSeconds;
        }

        if (options.DetermineSslEnabled())
        {
            factory.Ssl.Enabled = true;

            if (!options.Ssl.ValidateServerCertificate)
            {
                factory.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNotAvailable | SslPolicyErrors.RemoteCertificateChainErrors;
            }

            if (!options.Ssl.VerifyHostname)
            {
                factory.Ssl.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch;
            }
            else
            {
                factory.Ssl.ServerName = options.Ssl.ServerHostName;
            }

            if (!string.IsNullOrEmpty(options.Ssl.CertPath))
            {
                factory.Ssl.CertPath = options.Ssl.CertPath;
            }

            if (!string.IsNullOrEmpty(options.Ssl.CertPassPhrase))
            {
                factory.Ssl.CertPassphrase = options.Ssl.CertPassPhrase;
            }

            factory.Ssl.Version = options.Ssl.Algorithm;
        }

        if (options.ConnectionTimeout.HasValue)
        {
            int asMilliseconds = (int)options.ConnectionTimeout.Value.TotalMilliseconds;
            factory.RequestedConnectionTimeout = asMilliseconds;
        }
    }

    private RC.IModel GetChannel(ChannelCachingConnectionProxy connection, bool transactional)
    {
        SemaphoreSlim permits = null;

        if (ChannelCheckoutTimeout > 0)
        {
            permits = ObtainPermits(connection);
        }

        LinkedList<IChannelProxy> channelList = DetermineChannelList(connection, transactional);
        IChannelProxy channel = null;

        if (connection.IsOpen)
        {
            channel = FindOpenChannel(channelList, channel);

            if (channel != null)
            {
                Logger?.LogTrace("Found cached Rabbit Channel: {channel}", channel);
            }
        }

        if (channel == null)
        {
            try
            {
                channel = GetCachedChannelProxy(connection, channelList, transactional);
            }
            catch (Exception exception)
            {
                if (permits != null)
                {
                    permits.Release();

                    Logger?.LogDebug(exception, "Could not get channel; released permit for {connection}, remaining {permits}", connection,
                        permits.CurrentCount);
                }

                throw;
            }
        }

        return channel;
    }

    private SemaphoreSlim ObtainPermits(ChannelCachingConnectionProxy connection)
    {
        if (CheckoutPermits.TryGetValue(connection, out SemaphoreSlim permits))
        {
            try
            {
                if (!permits.Wait(ChannelCheckoutTimeout))
                {
                    throw new RabbitTimeoutException("No available channels");
                }

                Logger?.LogDebug("Acquired permit for {connection}, remaining {permits}", connection, permits.CurrentCount);
            }
            catch (ObjectDisposedException e)
            {
                throw new RabbitTimeoutException("Failure while acquiring a channel", e);
            }
        }
        else
        {
            throw new InvalidOperationException($"No permits map entry for {connection}");
        }

        return permits;
    }

    private IChannelProxy FindOpenChannel(LinkedList<IChannelProxy> channelList, IChannelProxy channelArg)
    {
        IChannelProxy channel = channelArg;

        lock (channelList)
        {
            while (channelList.Count > 0)
            {
                channel = channelList.First.Value;
                channelList.RemoveFirst();
                Logger?.LogTrace("{channel} retrieved from cache", channel);

                if (channel.IsOpen)
                {
                    break;
                }

                CleanUpClosedChannel(channel);
                channel = null;
            }
        }

        return channel;
    }

    private void CleanUpClosedChannel(IChannelProxy channel)
    {
        try
        {
            RC.IModel target = channel.TargetChannel;

            if (target != null)
            {
                target.Close();
                /*
                 *  To remove it from auto-recovery if so configured,
                 *  and nack any pending confirms if PublisherCallbackChannel.
                 */
            }
        }
        catch (AlreadyClosedException exception)
        {
            Logger?.LogTrace(exception, "{channel} is already closed", channel);
        }
        catch (TimeoutException exception)
        {
            Logger?.LogWarning(exception, "TimeoutException closing channel {channel}", channel);
        }
        catch (Exception exception)
        {
            Logger?.LogDebug(exception, "Unexpected Exception closing channel {channel}", channel);
        }
    }

    private LinkedList<IChannelProxy> DetermineChannelList(ChannelCachingConnectionProxy connection, bool transactional)
    {
        LinkedList<IChannelProxy> channelList;

        if (CacheMode == CachingMode.Channel)
        {
            channelList = transactional ? CachedChannelsTransactional : CachedChannelsNonTransactional;
        }
        else
        {
            if (transactional)
            {
                AllocatedConnectionTransactionalChannels.TryGetValue(connection, out channelList);
            }
            else
            {
                AllocatedConnectionNonTransactionalChannels.TryGetValue(connection, out channelList);
            }
        }

        if (channelList == null)
        {
            throw new InvalidOperationException($"No channel list for connection {connection}");
        }

        return channelList;
    }

    private IChannelProxy GetCachedChannelProxy(ChannelCachingConnectionProxy connection, LinkedList<IChannelProxy> channelList, bool transactional)
    {
        RC.IModel targetChannel = CreateBareChannel(connection, transactional);
        Logger?.LogDebug("Creating cached Rabbit Channel from {targetChannel}", targetChannel);
        ChannelListener.OnCreate(targetChannel, transactional);

        if (_confirmType == ConfirmType.Correlated || IsPublisherReturns)
        {
            return new CachedPublisherCallbackChannelProxy(this, connection, targetChannel, channelList, transactional,
                LoggerFactory?.CreateLogger<CachedPublisherCallbackChannelProxy>());
        }

        return new CachedChannelProxy(this, connection, targetChannel, channelList, transactional, LoggerFactory?.CreateLogger<CachedChannelProxy>());
    }

    private RC.IModel CreateBareChannel(ChannelCachingConnectionProxy connection, bool transactional)
    {
        if (CacheMode == CachingMode.Channel)
        {
            if (!Connection.IsOpen)
            {
                lock (_connectionMonitor)
                {
                    if (!Connection.IsOpen)
                    {
                        Connection.NotifyCloseIfNecessary();
                    }

                    if (!Connection.IsOpen)
                    {
                        Connection.Target = null;
                        CreateConnection();
                    }
                }
            }

            return DoCreateBareChannel(Connection, transactional);
        }

        if (CacheMode == CachingMode.Connection)
        {
            if (!connection.IsOpen)
            {
                lock (_connectionMonitor)
                {
                    AllocatedConnectionNonTransactionalChannels.TryGetValue(connection, out LinkedList<IChannelProxy> proxies);
                    proxies?.Clear();
                    connection.NotifyCloseIfNecessary();
                    RefreshProxyConnection(connection);
                }
            }

            return DoCreateBareChannel(connection, transactional);
        }

        return null;
    }

    private RC.IModel DoCreateBareChannel(ChannelCachingConnectionProxy conn, bool transactional)
    {
        RC.IModel channel = conn.CreateBareChannel(transactional);

        if (!ConfirmType.None.Equals(_confirmType))
        {
            try
            {
                channel.ConfirmSelect();
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Could not configure the channel to receive publisher confirms");
            }
        }

        if ((IsPublisherConfirms || IsPublisherReturns) && channel is not PublisherCallbackChannel)
        {
            channel = PublisherCallbackChannelFactory.CreateChannel(channel);
        }

        if (channel != null)
        {
            channel.ModelShutdown += ChannelShutdownCompleted;
        }

        return channel;
    }

    private long CurrentTimeMillis()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private IConnection GetConnectionFromCache()
    {
        ChannelCachingConnectionProxy cachedConnection = FindIdleConnection();
        long now = CurrentTimeMillis();

        if (cachedConnection == null && CountOpenConnections() >= ConnectionLimit)
        {
            cachedConnection = WaitForConnection(now);
        }

        if (cachedConnection == null)
        {
            if (CountOpenConnections() >= ConnectionLimit && CurrentTimeMillis() - now >= ChannelCheckoutTimeout)
            {
                throw new RabbitTimeoutException("Timed out attempting to get a connection");
            }

            cachedConnection = new ChannelCachingConnectionProxy(this, CreateBareConnection(), Logger);
            Logger?.LogDebug("Adding new connection '{connection}'", cachedConnection);

            AllocatedConnections.Add(cachedConnection);

            var nonTrans = new LinkedList<IChannelProxy>();
            var trans = new LinkedList<IChannelProxy>();
            AllocatedConnectionNonTransactionalChannels[cachedConnection] = nonTrans;
            AllocatedConnectionTransactionalChannels[cachedConnection] = trans;

            _channelHighWaterMarks[RuntimeHelpers.GetHashCode(nonTrans)] = new AtomicInteger();
            _channelHighWaterMarks[RuntimeHelpers.GetHashCode(trans)] = new AtomicInteger();

            CheckoutPermits[cachedConnection] = new SemaphoreSlim(ChannelCacheSize);

            ConnectionListener.OnCreate(cachedConnection);
        }
        else if (!cachedConnection.IsOpen)
        {
            try
            {
                RefreshProxyConnection(cachedConnection);
            }
            catch (Exception)
            {
                IdleConnections.AddLast(cachedConnection);
            }
        }
        else
        {
            Logger?.LogDebug("Obtained connection '{connection}' from cache", cachedConnection);
        }

        return cachedConnection;
    }

    private ChannelCachingConnectionProxy WaitForConnection(long now)
    {
        ChannelCachingConnectionProxy cachedConnection = null;

        while (cachedConnection == null && CurrentTimeMillis() - now < ChannelCheckoutTimeout)
        {
            if (CountOpenConnections() >= ConnectionLimit)
            {
                try
                {
                    if (Monitor.Wait(_connectionMonitor, ChannelCheckoutTimeout))
                    {
                        cachedConnection = FindIdleConnection();
                    }
                }
                catch (Exception e)
                {
                    Logger?.LogError(e, "Exception while waiting for a connection");
                    throw new RabbitException("Interrupted while waiting for a connection", e);
                }
            }
        }

        return cachedConnection;
    }

    private ChannelCachingConnectionProxy FindIdleConnection()
    {
        ChannelCachingConnectionProxy cachedConnection = null;
        ChannelCachingConnectionProxy lastIdle = IdleConnections.Last?.Value;

        while (cachedConnection == null)
        {
            cachedConnection = IdleConnections.First?.Value;

            if (cachedConnection != null)
            {
                IdleConnections.RemoveFirst();

                if (!cachedConnection.IsOpen)
                {
                    Logger?.LogDebug("Skipping closed connection '{connection}'", cachedConnection);
                    cachedConnection.NotifyCloseIfNecessary();
                    IdleConnections.AddLast(cachedConnection);

                    if (cachedConnection == lastIdle)
                    {
                        // all of the idle connections are closed.
                        cachedConnection = IdleConnections.First?.Value;

                        if (cachedConnection != null)
                        {
                            IdleConnections.RemoveFirst();
                        }

                        break;
                    }

                    cachedConnection = null;
                }
            }
            else
            {
                break;
            }
        }

        return cachedConnection;
    }

    private void RefreshProxyConnection(ChannelCachingConnectionProxy connection)
    {
        connection.Dispose();
        connection.NotifyCloseIfNecessary();
        connection.Target = CreateBareConnection();
        connection.CloseNotified = 0;
        ConnectionListener.OnCreate(connection);
        Logger?.LogDebug("Refreshed existing connection '{connection}'", connection);
    }

    private void InitCacheWaterMarks()
    {
        _channelHighWaterMarks[RuntimeHelpers.GetHashCode(CachedChannelsNonTransactional)] = new AtomicInteger();
        _channelHighWaterMarks[RuntimeHelpers.GetHashCode(CachedChannelsTransactional)] = new AtomicInteger();
    }

    public enum CachingMode
    {
        Channel,
        Connection
    }

    public enum ConfirmType
    {
        Simple,
        Correlated,
        None
    }

    internal sealed class CachedPublisherCallbackChannelProxy : CachedChannelProxy, IPublisherCallbackChannel
    {
        private IPublisherCallbackChannel PublisherCallbackChannel => (IPublisherCallbackChannel)target;

        public RC.IModel Channel => target;

        public CachedPublisherCallbackChannelProxy(CachingConnectionFactory factory, ChannelCachingConnectionProxy connection, RC.IModel target,
            LinkedList<IChannelProxy> channelList, bool transactional, ILogger logger)
            : base(factory, connection, target, channelList, transactional, logger)
        {
        }

        public void AddListener(IPublisherCallbackChannel.IListener listener)
        {
            PublisherCallbackChannel.AddListener(listener);
        }

        public IList<PendingConfirm> Expire(IPublisherCallbackChannel.IListener listener, long cutoffTime)
        {
            return PublisherCallbackChannel.Expire(listener, cutoffTime);
        }

        public int GetPendingConfirmsCount(IPublisherCallbackChannel.IListener listener)
        {
            return PublisherCallbackChannel.GetPendingConfirmsCount(listener);
        }

        public int GetPendingConfirmsCount()
        {
            return PublisherCallbackChannel.GetPendingConfirmsCount();
        }

        public void AddPendingConfirm(IPublisherCallbackChannel.IListener listener, ulong sequence, PendingConfirm pendingConfirm)
        {
            PublisherCallbackChannel.AddPendingConfirm(listener, sequence, pendingConfirm);
        }

        public void SetAfterAckCallback(Action<RC.IModel> callback)
        {
            PublisherCallbackChannel.SetAfterAckCallback(callback);
        }

        public override string ToString()
        {
            return $"Cached Rabbit Channel: {target}, conn: {TheConnection}";
        }

        protected override void ReturnToCache()
        {
            if (Factory._active && PublisherConfirms)
            {
                TheConnection.ChannelsAwaitingAcks[target] = this;

                ((IPublisherCallbackChannel)target).SetAfterAckCallback(c =>
                {
                    TheConnection.ChannelsAwaitingAcks.Remove(c);
                    DoReturnToCache();
                });
            }
            else
            {
                DoReturnToCache();
            }
        }
    }

    internal class CachedChannelProxy : IChannelProxy
    {
        protected const int AsyncCloseTimeout = 5_000;
        protected readonly CachingConnectionFactory Factory;
        protected readonly ChannelCachingConnectionProxy TheConnection;
        protected readonly LinkedList<IChannelProxy> ChannelList;
        protected readonly int ChannelListIdentity;
        protected readonly object TargetMonitor = new();
        protected readonly bool Transactional;
        protected readonly bool ConfirmSelected;
        protected readonly bool PublisherConfirms;
        protected readonly ILogger Logger;
        protected RC.IModel target;
        protected bool txStarted;

        public RC.IModel TargetChannel => target;

        public bool IsTransactional => Transactional;

        public bool IsConfirmSelected => ConfirmSelected;

        public int ChannelNumber
        {
            get
            {
                try
                {
                    PreInvoke();
                    return target.ChannelNumber;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public RC.ShutdownEventArgs CloseReason
        {
            get
            {
                try
                {
                    PreInvoke();
                    return target.CloseReason;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public RC.IBasicConsumer DefaultConsumer
        {
            get
            {
                try
                {
                    PreInvoke();
                    return target.DefaultConsumer;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            set
            {
                try
                {
                    PreInvoke();
                    target.DefaultConsumer = value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public bool IsClosed => target == null || target.IsClosed;

        public bool IsOpen => target != null && target.IsOpen;

        public ulong NextPublishSeqNo
        {
            get
            {
                try
                {
                    PreInvoke();
                    return target.NextPublishSeqNo;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public TimeSpan ContinuationTimeout
        {
            get
            {
                try
                {
                    PreInvoke();
                    return target.ContinuationTimeout;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            set
            {
                try
                {
                    PreInvoke();
                    target.ContinuationTimeout = value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<BasicAckEventArgs> BasicAcks
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.BasicAcks += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.BasicAcks -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<BasicNackEventArgs> BasicNacks
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.BasicNacks += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.BasicNacks -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<EventArgs> BasicRecoverOk
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.BasicRecoverOk += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.BasicRecoverOk -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<BasicReturnEventArgs> BasicReturn
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.BasicReturn += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.BasicReturn -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<CallbackExceptionEventArgs> CallbackException
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.CallbackException += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.CallbackException -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<FlowControlEventArgs> FlowControl
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.FlowControl += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.FlowControl -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public event EventHandler<RC.ShutdownEventArgs> ModelShutdown
        {
            add
            {
                try
                {
                    PreInvoke();
                    target.ModelShutdown += value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            remove
            {
                try
                {
                    PreInvoke();
                    target.ModelShutdown -= value;
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
        }

        public CachedChannelProxy(CachingConnectionFactory factory, ChannelCachingConnectionProxy connection, RC.IModel target,
            LinkedList<IChannelProxy> channelList, bool transactional, ILogger logger)
        {
            Factory = factory;
            TheConnection = connection;
            this.target = target;
            ChannelList = channelList;
            ChannelListIdentity = RuntimeHelpers.GetHashCode(channelList);
            Transactional = transactional;
            ConfirmSelected = Factory.IsSimplePublisherConfirms;
            PublisherConfirms = Factory.IsPublisherConfirms;
            Logger = logger;
        }

        public void Abort()
        {
            try
            {
                PreInvoke();
                target.Abort();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void Abort(ushort replyCode, string replyText)
        {
            try
            {
                PreInvoke();
                target.Abort(replyCode, replyText);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            try
            {
                if (target == null || !target.IsOpen)
                {
                    throw new InvalidOperationException("Channel closed; cannot ack/nack");
                }

                PreInvoke();
                target.BasicAck(deliveryTag, multiple);

                if (Transactional)
                {
                    txStarted = true;
                }
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicCancel(string consumerTag)
        {
            try
            {
                PreInvoke();
                target.BasicCancel(consumerTag);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments,
            RC.IBasicConsumer consumer)
        {
            try
            {
                PreInvoke();
                return target.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public RC.BasicGetResult BasicGet(string queue, bool autoAck)
        {
            try
            {
                PreInvoke();
                return target.BasicGet(queue, autoAck);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            try
            {
                if (target == null || !target.IsOpen)
                {
                    throw new InvalidOperationException("Channel closed; cannot ack/nack");
                }

                PreInvoke();
                target.BasicNack(deliveryTag, multiple, requeue);

                if (Transactional)
                {
                    txStarted = true;
                }
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, RC.IBasicProperties basicProperties, byte[] body)
        {
            try
            {
                PreInvoke();
                target.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);

                if (Transactional)
                {
                    txStarted = true;
                }
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            try
            {
                PreInvoke();
                target.BasicQos(prefetchSize, prefetchCount, global);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicRecover(bool requeue)
        {
            try
            {
                PreInvoke();
                target.BasicRecover(requeue);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicRecoverAsync(bool requeue)
        {
            try
            {
                PreInvoke();
                target.BasicRecoverAsync(requeue);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            try
            {
                if (target == null || !target.IsOpen)
                {
                    throw new InvalidOperationException("Channel closed; cannot ack/nack");
                }

                PreInvoke();
                target.BasicReject(deliveryTag, requeue);

                if (Transactional)
                {
                    txStarted = true;
                }
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void Close()
        {
            DoClose();
        }

        public void Close(ushort replyCode, string replyText)
        {
            DoClose();
        }

        public void ConfirmSelect()
        {
            try
            {
                PreInvoke();
                target.ConfirmSelect();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public uint ConsumerCount(string queue)
        {
            try
            {
                PreInvoke();
                return target.ConsumerCount(queue);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public RC.IBasicProperties CreateBasicProperties()
        {
            try
            {
                PreInvoke();
                return target.CreateBasicProperties();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public RC.IBasicPublishBatch CreateBasicPublishBatch()
        {
            try
            {
                PreInvoke();
                return target.CreateBasicPublishBatch();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.ExchangeBind(destination, source, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.ExchangeBindNoWait(destination, source, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            try
            {
                PreInvoke();
                target.ExchangeDeclarePassive(exchange);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            try
            {
                PreInvoke();
                target.ExchangeDelete(exchange, ifUnused);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeDeleteNoWait(string exchange, bool ifUnused)
        {
            try
            {
                PreInvoke();
                target.ExchangeDeleteNoWait(exchange, ifUnused);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.ExchangeUnbind(destination, source, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.ExchangeUnbindNoWait(destination, source, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public uint MessageCount(string queue)
        {
            try
            {
                PreInvoke();
                return target.MessageCount(queue);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.QueueBind(queue, exchange, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.QueueBindNoWait(queue, exchange, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public RC.QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                return target.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public RC.QueueDeclareOk QueueDeclarePassive(string queue)
        {
            try
            {
                PreInvoke();
                return target.QueueDeclarePassive(queue);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            try
            {
                PreInvoke();
                return target.QueueDelete(queue, ifUnused, ifEmpty);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
        {
            try
            {
                PreInvoke();
                target.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public uint QueuePurge(string queue)
        {
            try
            {
                PreInvoke();
                return target.QueuePurge(queue);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            try
            {
                PreInvoke();
                target.QueueUnbind(queue, exchange, routingKey, arguments);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void TxCommit()
        {
            try
            {
                PreInvoke();
                target.TxCommit();

                if (Transactional)
                {
                    txStarted = false;
                }
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void TxRollback()
        {
            try
            {
                PreInvoke();
                target.TxRollback();

                if (Transactional)
                {
                    txStarted = false;
                }
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void TxSelect()
        {
            try
            {
                PreInvoke();

                if (!Transactional)
                {
                    throw new InvalidOperationException("Cannot start transaction on non-transactional channel");
                }

                target.TxSelect();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public bool WaitForConfirms()
        {
            try
            {
                PreInvoke();
                return target.WaitForConfirms();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public bool WaitForConfirms(TimeSpan timeout)
        {
            try
            {
                PreInvoke();
                return target.WaitForConfirms(timeout);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            try
            {
                PreInvoke();
                return target.WaitForConfirms(timeout, out timedOut);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void WaitForConfirmsOrDie()
        {
            try
            {
                PreInvoke();
                target.WaitForConfirmsOrDie();
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            try
            {
                PreInvoke();
                target.WaitForConfirmsOrDie(timeout);
            }
            catch (Exception e)
            {
                PostException(e);
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public override string ToString()
        {
            return $"Cached Rabbit Channel: {target}, conn: {TheConnection}";
        }

        protected virtual void PostException(Exception e)
        {
            if (target == null || !target.IsOpen)
            {
                // Basic re-connection logic...
                Logger?.LogDebug(e, "Detected closed channel on exception. Re-initializing: {target}", target);
                target = null;

                lock (TargetMonitor)
                {
                    target ??= Factory.CreateBareChannel(TheConnection, Transactional);
                }
            }
        }

        protected virtual void PreInvoke()
        {
            if (target == null || !target.IsOpen)
            {
                if (target is IPublisherCallbackChannel)
                {
                    target.Close();
                    throw new RabbitException("PublisherCallbackChannel is closed");
                }

                if (txStarted)
                {
                    txStarted = false;
                    throw new InvalidOperationException("Channel closed during transaction");
                }

                target = null;
            }

            lock (TargetMonitor)
            {
                target ??= Factory.CreateBareChannel(TheConnection, Transactional);
            }
        }

        protected virtual void ReleasePermitIfNecessary()
        {
            if (Factory.ChannelCheckoutTimeout > 0)
            {
                /*
                 *  Only release a permit if this is a normal close; if the channel is
                 *  in the list, it means we're closing a cached channel (for which a permit
                 *  has already been released).
                 */
                lock (ChannelList)
                {
                    if (ChannelList.Contains(this))
                    {
                        return;
                    }
                }

                if (Factory.CheckoutPermits.TryGetValue(TheConnection, out SemaphoreSlim permits))
                {
                    permits.Release();
                    Logger?.LogDebug("Released permit for '{connection}', remaining: {permits}", TheConnection, permits.CurrentCount);
                }
                else
                {
                    Logger?.LogError("LEAKAGE: No permits map entry for {connection}", TheConnection);
                }
            }
        }

        protected virtual void ReturnToCache()
        {
            // if (_factory._active && _publisherConfirms && this is IPublisherCallbackChannel)
            // {
            //    _theConnection._channelsAwaitingAcks[_target] = this;
            //    ((IPublisherCallbackChannel)_target)
            //            .SetAfterAckCallback(c => DoReturnToCache()); // _theConnection._channelsAwaitingAcks.Remove(c)));
            // }
            // else
            // {
            DoReturnToCache();

            // }
        }

        protected void DoReturnToCache()
        {
            // if (proxy != null)
            // {
            lock (ChannelList)
            {
                // Allow for multiple close calls...
                if (Factory._active)
                {
                    if (!ChannelList.Contains(this))
                    {
                        Logger?.LogTrace("Returning cached channel: {channel}", target);
                        ReleasePermitIfNecessary();
                        ChannelList.AddLast(this);
                        SetHighWaterMark();
                    }
                }
                else
                {
                    if (IsOpen)
                    {
                        try
                        {
                            PhysicalClose();
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError(e, "Exception while doing PhysicalClose()");
                        }
                    }
                }
            }

            // }
            // }
        }

        protected virtual void LogicalClose()
        {
            if (target == null)
            {
                return;
            }

            if (!target.IsOpen)
            {
                lock (TargetMonitor)
                {
                    if (!target.IsOpen)
                    {
                        if (target is IPublisherCallbackChannel)
                        {
                            target.Close(); // emit nacks if necessary
                        }

                        if (ChannelList.Contains(this))
                        {
                            ChannelList.Remove(this);
                        }
                        else
                        {
                            ReleasePermitIfNecessary();
                        }

                        target = null;
                        return;
                    }
                }
            }

            ReturnToCache();
        }

        protected void SetHighWaterMark()
        {
            if (Factory._channelHighWaterMarks.TryGetValue(ChannelListIdentity, out AtomicInteger hwm))
            {
                // No need for atomicity since we're synced on the channel list
                int prev = hwm.Value;
                int size = ChannelList.Count;

                if (size > prev)
                {
                    hwm.Value = size;
                }
            }
        }

        protected virtual void PhysicalClose()
        {
            Logger?.LogDebug("Closing cached channel: {channel}", target);

            if (target == null)
            {
                return;
            }

            bool asyncClose = false;

            try
            {
                if (Factory._active && (Factory.IsPublisherConfirms || Factory.IsPublisherReturns))
                {
                    asyncClose = true;
                    AsyncClose();
                }
                else
                {
                    target.Close();

                    // if (this.target instanceof AutorecoveringChannel) {
                    //    ClosingRecoveryListener.removeChannel((AutorecoveringChannel)this.target);
                    // }
                }
            }
            catch (AlreadyClosedException e)
            {
                Logger?.LogTrace(e, "{channel} is already closed", target);
            }
            finally
            {
                target = null;

                if (!asyncClose)
                {
                    ReleasePermitIfNecessary();
                }
            }
        }

        protected void AsyncClose()
        {
            // ExecutorService executorService = getChannelsExecutor();
            RC.IModel channel = target;

            // _factory._inFlightAsyncCloses.Add(channel);
            try
            {
                Task.Run(() =>
                {
                    Logger?.LogDebug("Starting AsyncClose processing");

                    try
                    {
                        if (Factory.IsPublisherConfirms)
                        {
                            channel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(AsyncCloseTimeout));
                        }
                        else
                        {
                            Thread.Sleep(AsyncCloseTimeout);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger?.LogError(e, "Exception in AsyncClose processing");
                    }
                    finally
                    {
                        try
                        {
                            channel.Close();
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError(e, "Exception in AsyncClose issued channel close");
                        }
                        finally
                        {
                            // _factory._inFlightAsyncCloses.release(channel);
                            ReleasePermitIfNecessary();
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Exception while running AsyncClose processing");

                // _factory._inFlightAsyncCloses.release(channel);
            }
        }

        protected virtual void DoClose()
        {
            if (Factory._active)
            {
                lock (ChannelList)
                {
                    if (Factory._active && !RabbitUtils.IsPhysicalCloseRequired() &&
                        (ChannelList.Count < Factory.ChannelCacheSize || ChannelList.Contains(this)))
                    {
                        LogicalClose();
                        return;
                    }
                }
            }

            PhysicalClose();
        }
    }

    internal sealed class ChannelCachingConnectionProxy : IConnectionProxy
    {
        private readonly CachingConnectionFactory _factory;
        private readonly ILogger _logger;
        internal readonly Dictionary<RC.IModel, IChannelProxy> ChannelsAwaitingAcks = new();
        internal int CloseNotified;

        internal IConnection Target { get; set; }

        public bool IsOpen => Target != null && Target.IsOpen;

        public IConnection TargetConnection => Target;

        public RC.IConnection Connection => Target.Connection;

        public int LocalPort
        {
            get
            {
                IConnection target = Target;

                if (target != null)
                {
                    return target.LocalPort;
                }

                return 0;
            }
        }

        public ChannelCachingConnectionProxy(CachingConnectionFactory factory, IConnection target, ILogger logger)
        {
            Target = target;
            _factory = factory;
            _logger = logger;
        }

        public RC.IModel CreateChannel(bool transactional = false)
        {
            return _factory.GetChannel(this, transactional);
        }

        public RC.IModel CreateBareChannel(bool transactional)
        {
            if (Target == null)
            {
                throw new InvalidOperationException("Can't create channel - no target connection.");
            }

            return Target.CreateChannel(transactional);
        }

        public void AddBlockedListener(IBlockedListener listener)
        {
            if (Target == null)
            {
                throw new InvalidOperationException("Can't add blocked listener - no target connection.");
            }

            Target.AddBlockedListener(listener);
        }

        public bool RemoveBlockedListener(IBlockedListener listener)
        {
            if (Target == null)
            {
                throw new InvalidOperationException("Can't remove blocked listener - no target connection.");
            }

            return Target.RemoveBlockedListener(listener);
        }

        public void Close()
        {
            if (_factory.CacheMode == CachingMode.Connection)
            {
                lock (_factory._connectionMonitor)
                {
                    if (!_factory.IdleConnections.Contains(this))
                    {
                        if (!IsOpen || CountOpenIdleConnections() >= _factory.ConnectionCacheSize)
                        {
                            _logger?.LogDebug("Completely closing connection '{connection}'", this);
                            Dispose();
                        }

                        _logger?.LogDebug("Returning connection '{connection}' to cache", this);

                        _factory.IdleConnections.AddLast(this);
                        int idleConnectionsSize = _factory.IdleConnections.Count;

                        if (_factory._connectionHighWaterMark.Value < idleConnectionsSize)
                        {
                            _factory._connectionHighWaterMark.Value = idleConnectionsSize;
                        }

                        Monitor.PulseAll(_factory._connectionMonitor);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_factory.CacheMode == CachingMode.Channel)
            {
                _factory.Reset(_factory.CachedChannelsNonTransactional, _factory.CachedChannelsTransactional, ChannelsAwaitingAcks);
            }
            else
            {
                _factory.AllocatedConnectionNonTransactionalChannels.TryGetValue(this, out LinkedList<IChannelProxy> nonTrans);
                _factory.AllocatedConnectionTransactionalChannels.TryGetValue(this, out LinkedList<IChannelProxy> trans);
                _factory.Reset(nonTrans, trans, ChannelsAwaitingAcks);
            }

            if (Target != null)
            {
                RabbitUtils.CloseConnection(Target, _logger);
                NotifyCloseIfNecessary();
            }

            Target = null;
        }

        public void NotifyCloseIfNecessary()
        {
            if (Interlocked.CompareExchange(ref CloseNotified, 1, 0) == 0)
            {
                _factory.ConnectionListener.OnClose(this);
            }
        }

        public override string ToString()
        {
            return
                $"Proxy@{RuntimeHelpers.GetHashCode(this)} {(_factory.CacheMode == CachingMode.Channel ? "Shared " : "Dedicated ")}Rabbit Connection: {Target}";
        }

        private int CountOpenIdleConnections()
        {
            int n = 0;

            foreach (ChannelCachingConnectionProxy proxy in _factory.IdleConnections)
            {
                if (proxy.IsOpen)
                {
                    n++;
                }
            }

            return n;
        }
    }

    private sealed class DefaultChannelCloseLogger : IConditionalExceptionLogger
    {
        public void Log(ILogger logger, string message, object cause)
        {
            logger?.LogError("Unexpected invocation of {type}, with {message}:{cause} ", GetType(), message, cause);
        }
    }
}
