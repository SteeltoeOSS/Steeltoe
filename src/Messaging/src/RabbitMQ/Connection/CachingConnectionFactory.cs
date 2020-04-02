﻿// Copyright 2017 the original author or authors.
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
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class CachingConnectionFactory : AbstractConnectionFactory, IShutdownListener
    {
        public enum CachingMode
        {
            CHANNEL,
            CONNECTION
        }

        public enum ConfirmType
        {
            SIMPLE,
            CORRELATED,
            NONE
        }

        // Internal for unit tests
        internal readonly Dictionary<IConnection, SemaphoreSlim> _checkoutPermits = new Dictionary<IConnection, SemaphoreSlim>();
        internal readonly LinkedList<IChannelProxy> _cachedChannelsNonTransactional = new LinkedList<IChannelProxy>();
        internal readonly LinkedList<IChannelProxy> _cachedChannelsTransactional = new LinkedList<IChannelProxy>();
        internal readonly HashSet<ChannelCachingConnectionProxy> _allocatedConnections = new HashSet<ChannelCachingConnectionProxy>();
        internal readonly LinkedList<ChannelCachingConnectionProxy> _idleConnections = new LinkedList<ChannelCachingConnectionProxy>();
        internal readonly Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> _allocatedConnectionNonTransactionalChannels = new Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>>();
        internal readonly Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> _allocatedConnectionTransactionalChannels = new Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>>();

        private const int DEFAULT_CHANNEL_CACHE_SIZE = 25;
        private readonly ChannelCachingConnectionProxy _connection;
        private readonly object _connectionMonitor = new object();
        private readonly Dictionary<int, AtomicInteger> _channelHighWaterMarks = new Dictionary<int, AtomicInteger>();
        private readonly AtomicInteger _connectionHighWaterMark = new AtomicInteger();
        private readonly IOptionsMonitor<RabbitOptions> _optionsMonitor;

        private int _channelCacheSize = DEFAULT_CHANNEL_CACHE_SIZE;
        private int _connectionCacheSize = 1;
        private int _connectionLimit = int.MaxValue;
        private bool _publisherReturns;
        private ConfirmType _confirmType = ConfirmType.NONE;
        private int _channelCheckoutTimeout = 0;
        private IConditionalExceptionLogger _closeExceptionLogger = new DefaultChannelCloseLogger();
        private bool _stopped;
        private bool _active = true;

        public CachingConnectionFactory(ILogger logger = null)
            : this((string)null, -1, CachingMode.CHANNEL, logger)
        {
        }

        public CachingConnectionFactory(IOptionsMonitor<RabbitOptions> optionsMonitor, ILogger logger = null)
            : base(NewRabbitConnectionFactory(), logger)
        {
            _optionsMonitor = optionsMonitor;
            ConfigureRabbitConnectionFactory(Options);
            PublisherConnectionFactory = new CachingConnectionFactory(_rabbitConnectionFactory, true);
            _connection = new ChannelCachingConnectionProxy(this, null, logger);
            Configure(Options);
            InitCacheWaterMarks();
        }

        public CachingConnectionFactory(string hostname, ILogger logger = null)
            : this(hostname, -1, CachingMode.CHANNEL, logger)
        {
        }

        public CachingConnectionFactory(int port, ILogger logger = null)
            : this(null, port, CachingMode.CHANNEL, logger)
        {
        }

        public CachingConnectionFactory(string hostNameArg, int port, CachingMode cachingMode = CachingMode.CHANNEL, ILogger logger = null)
            : base(NewRabbitConnectionFactory(), logger)
        {
            var hostname = hostNameArg;
            if (string.IsNullOrEmpty(hostname))
            {
                hostname = GetDefaultHostName();
            }

            Host = hostname;
            Port = port;
            PublisherConnectionFactory = new CachingConnectionFactory(_rabbitConnectionFactory, true);
            _connection = new ChannelCachingConnectionProxy(this, null, logger);
            CacheMode = cachingMode;
            InitCacheWaterMarks();
        }

        public CachingConnectionFactory(Uri uri, ILogger logger = null)
            : this(uri, CachingMode.CHANNEL, logger)
        {
        }

        public CachingConnectionFactory(Uri uri, CachingMode cachingMode = CachingMode.CHANNEL, ILogger logger = null)
            : base(NewRabbitConnectionFactory(), logger)
        {
            CacheMode = cachingMode;
            Uri = uri;
            PublisherConnectionFactory = new CachingConnectionFactory(_rabbitConnectionFactory, true);
            _connection = new ChannelCachingConnectionProxy(this, null, logger);
            InitCacheWaterMarks();
        }

        protected internal CachingConnectionFactory(RabbitMQ.Client.IConnectionFactory rabbitConnectionFactory, ILogger logger = null)
            : this(rabbitConnectionFactory, false, CachingMode.CHANNEL, logger)
        {
        }

        protected internal CachingConnectionFactory(RabbitMQ.Client.IConnectionFactory rabbitConnectionFactory, bool isPublisherFactory, CachingMode cachingMode = CachingMode.CHANNEL, ILogger logger = null)
            : base(rabbitConnectionFactory, logger)
        {
            CacheMode = cachingMode;
            _connection = new ChannelCachingConnectionProxy(this, null, logger);
            if (!isPublisherFactory)
            {
                if (RabbitConnectionFactory != null && RabbitConnectionFactory.AutomaticRecoveryEnabled)
                {
                    RabbitConnectionFactory.AutomaticRecoveryEnabled = false;
                    logger?.LogWarning("***\nAutomatic Recovery was Enabled in the provided connection factory;\n"
                            + "while Steeltoe is generally compatible with this feature, there\n"
                            + "are some corner cases where problems arise. Steeltoe\n"
                            + "prefers to use its own recovery mechanisms; when this option is true, you may receive\n"
                            + "odd Exception's until the connection is recovered.\n");
                }

                PublisherConnectionFactory = new CachingConnectionFactory(_rabbitConnectionFactory, true, cachingMode, logger);
            }
            else
            {
                PublisherConnectionFactory = null;
            }

            InitCacheWaterMarks();
        }

        private static ConnectionFactory NewRabbitConnectionFactory()
        {
            var connectionFactory = new ConnectionFactory
            {
                AutomaticRecoveryEnabled = false
            };
            return connectionFactory;
        }

        public CachingConnectionFactory PublisherCachingConnectionFactory
        {
            get
            {
                return (CachingConnectionFactory)PublisherConnectionFactory;
            }

            set
            {
                PublisherConnectionFactory = value;
            }
        }

        public int ChannelCacheSize
        {
            get
            {
                return _channelCacheSize;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(ChannelCacheSize));
                }

                _channelCacheSize = value;
                if (PublisherConnectionFactory != null)
                {
                    PublisherCachingConnectionFactory.ChannelCacheSize = value;
                }
            }
        }

        public CachingMode CacheMode { get; private set; }

        public int ConnectionCacheSize
        {
            get
            {
                return _connectionCacheSize;
            }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(ConnectionCacheSize));
                }

                if (CacheMode == CachingMode.CHANNEL && value != 1)
                {
                    throw new ArgumentException("When the cache mode is 'CHANNEL', the connection cache size cannot be configured.");
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
            get
            {
                return _connectionLimit;
            }

            set
            {
                // TODO: Check mode
                if (value < 1)
                {
                    throw new ArgumentException(nameof(ConnectionLimit));
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
            get
            {
                return _publisherReturns;
            }

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
            get
            {
                return _confirmType;
            }

            set
            {
                _confirmType = value;
                if (PublisherConnectionFactory != null)
                {
                    PublisherCachingConnectionFactory.PublisherConfirmType = value;
                }
            }
        }

        public override bool IsPublisherConfirms
        {
            get { return _confirmType == ConfirmType.CORRELATED; }
        }

        public override bool IsSimplePublisherConfirms
        {
            get { return _confirmType == ConfirmType.SIMPLE; }
        }

        public int ChannelCheckoutTimeout
        {
            get
            {
                return _channelCheckoutTimeout;
            }

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
            get
            {
                return _closeExceptionLogger;
            }

            set
            {
                _closeExceptionLogger = value;
                if (PublisherConnectionFactory != null)
                {
                    PublisherCachingConnectionFactory.CloseExceptionLogger = value;
                }
            }
        }

        public IPublisherCallbackChannelFactory PublisherCallbackChannelFactory { get; set; } = new DefaultPublisherCallbackFactory();

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

        public override void SetConnectionListeners(List<IConnectionListener> listeners)
        {
            base.SetConnectionListeners(listeners);
            if (_connection.Target != null)
            {
                ConnectionListener.OnCreate(_connection);
            }
        }

        public override void AddConnectionListener(IConnectionListener connectionListener)
        {
            base.AddConnectionListener(connectionListener);
            if (_connection.Target != null)
            {
                connectionListener.OnCreate(_connection);
            }
        }

        public void ShutdownCompleted(object sender, ShutdownEventArgs args)
        {
            _closeExceptionLogger.Log(_logger, "Channel shutdown", args.Cause);
            int protocolClassId = args.ClassId;
            if (protocolClassId == RabbitUtils.ChannelClose_ClassId)
            {
                ChannelListener.OnShutDown(args);
            }
            else if (protocolClassId == RabbitUtils.ConnectionClose_ClassId)
            {
                ConnectionListener.OnShutDown(args);
            }
        }

        public override IConnection CreateConnection()
        {
            if (_stopped)
            {
                throw new AmqpApplicationContextClosedException("The ConnectionFactory is disposed and can no longer create connections.");
            }

            lock (_connectionMonitor)
            {
                if (CacheMode == CachingMode.CHANNEL)
                {
                    if (_connection.Target == null)
                    {
                        _connection.Target = CreateBareConnection();

                        // invoke the listener *after* this.connection is assigned
                        if (!_checkoutPermits.ContainsKey(_connection))
                        {
                            _checkoutPermits.Add(_connection, new SemaphoreSlim(ChannelCacheSize));
                        }

                        _connection._closeNotified = 0;
                        ConnectionListener.OnCreate(_connection);
                    }

                    return _connection;
                }
                else if (CacheMode == CachingMode.CONNECTION)
                {
                    return GetConnectionFromCache();
                }
            }

            return null; // NOSONAR - never reach here - exceptions
        }

        public override void Destroy()
        {
            base.Destroy();
            ResetConnection();
        }

        public override void Dispose()
        {
            Destroy();
            _stopped = true;
        }

        // Used in unit test
        internal int CountOpenConnections()
        {
            var n = 0;
            foreach (var proxy in _allocatedConnections)
            {
                if (proxy.IsOpen)
                {
                    n++;
                }
            }

            return n;
        }

        #region Protected
        protected void ResetConnection()
        {
            lock (_connectionMonitor)
            {
                if (_connection.Target != null)
                {
                    _connection.Dispose();
                }

                foreach (var c in _allocatedConnections)
                {
                    c.Dispose();
                }

                foreach (var c in _channelHighWaterMarks)
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

        protected void Reset(LinkedList<IChannelProxy> channels, LinkedList<IChannelProxy> txChannels, Dictionary<IModel, IChannelProxy> channelsAwaitingAcks)
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
            foreach (var channel in theChannels)
            {
                try
                {
                    channel.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogTrace("Could not close cached Rabbit Channel", ex);
                }
            }
        }
        #endregion Protected

        #region Private
        private void Configure(RabbitOptions options)
        {
            SetAddresses(options.DetermineAddresses());
            IsPublisherConfirms = options.PublisherConfirms;
            IsPublisherReturns = options.PublisherReturns;
            var cacheChannel = options.Cache.Channel;
            if (cacheChannel.Size.HasValue)
            {
                ChannelCacheSize = cacheChannel.Size.Value;
            }

            if (cacheChannel.CheckoutTimeout.HasValue)
            {
                var asMilli = (int)cacheChannel.CheckoutTimeout.Value.TotalMilliseconds;
                ChannelCheckoutTimeout = asMilli;
            }

            var cacheConnection = options.Cache.Connection;
            CacheMode = cacheConnection.Mode;
            if (cacheConnection.Size.HasValue)
            {
                ConnectionCacheSize = cacheConnection.Size.Value;
            }
        }

        private void ConfigureRabbitConnectionFactory(RabbitOptions options)
        {
            var factory = _rabbitConnectionFactory as ConnectionFactory;
            var host = options.DetermineHost();
            if (host != null)
            {
                factory.HostName = host;
            }

            factory.Port = options.DeterminePort();
            var userName = options.DetermineUsername();
            if (userName != null)
            {
                factory.UserName = userName;
            }

            var password = options.DeterminePassword();
            if (password != null)
            {
                factory.Password = password;
            }

            var virtualHost = options.DetermineVirtualHost();
            if (virtualHost != null)
            {
                factory.VirtualHost = virtualHost;
            }

            if (options.RequestedHeartbeat.HasValue)
            {
                var asShortSeconds = (ushort)options.RequestedHeartbeat.Value.TotalSeconds;
                factory.RequestedHeartbeat = asShortSeconds;
            }

            if (options.Ssl.Enabled)
            {
                factory.Ssl.Enabled = true;

                // TODO: More
                // map.from(ssl::getAlgorithm).whenNonNull().to(factory::setSslAlgorithm);
                // map.from(ssl::getKeyStoreType).to(factory::setKeyStoreType);
                // map.from(ssl::getKeyStore).to(factory::setKeyStore);
                // map.from(ssl::getKeyStorePassword).to(factory::setKeyStorePassphrase);
                // map.from(ssl::getTrustStoreType).to(factory::setTrustStoreType);
                // map.from(ssl::getTrustStore).to(factory::setTrustStore);
                // map.from(ssl::getTrustStorePassword).to(factory::setTrustStorePassphrase);
                // map.from(ssl::isValidateServerCertificate).to((validate)->factory
                //        .setSkipServerCertificateValidation(!validate));
                // map.from(ssl::getVerifyHostname)
                //        .to(factory::setEnableHostnameVerification);
                // setUpSSL(); from FactoryBean
            }

            if (options.ConnectionTimeout.HasValue)
            {
                var asMilliseconds = (int)options.ConnectionTimeout.Value.TotalMilliseconds;
                factory.RequestedConnectionTimeout = asMilliseconds;
            }
        }

        private IModel GetChannel(ChannelCachingConnectionProxy connection, bool transactional)
        {
            SemaphoreSlim permits = null;
            if (ChannelCheckoutTimeout > 0)
            {
                permits = ObtainPermits(connection);
            }

            var channelList = DetermineChannelList(connection, transactional);
            IChannelProxy channel = null;
            if (connection.IsOpen)
            {
                channel = FindOpenChannel(channelList, channel);
                if (channel != null)
                {
                    _logger?.LogTrace("Found cached Rabbit Channel: " + channel.ToString());
                }
            }

            if (channel == null)
            {
                try
                {
                    channel = GetCachedChannelProxy(connection, channelList, transactional);
                }
                catch (Exception)
                {
                    if (permits != null)
                    {
                        permits.Release();
                        _logger?.LogDebug("Could not get channel; released permit for " + connection + ", remaining:" + permits.CurrentCount);
                    }

                    throw;
                }
            }

            return channel;
        }

        private SemaphoreSlim ObtainPermits(ChannelCachingConnectionProxy connection)
        {
            if (_checkoutPermits.TryGetValue(connection, out var permits))
            {
                try
                {
                    if (!permits.Wait(ChannelCheckoutTimeout))
                    {
                        throw new AmqpTimeoutException("No available channels");
                    }

                    _logger?.LogDebug("Acquired permit for " + connection + ", remaining:" + permits.CurrentCount);
                }
                catch (ObjectDisposedException e)
                {
                    throw new AmqpTimeoutException("Failure while acquiring a channel", e);
                }
            }
            else
            {
                throw new InvalidOperationException("No permits map entry for " + connection);
            }

            return permits;
        }

        private IChannelProxy FindOpenChannel(LinkedList<IChannelProxy> channelList, IChannelProxy channelArg)
        {
            var channel = channelArg;
            lock (channelList)
            {
                while (channelList.Count > 0)
                {
                    channel = channelList.First.Value;
                    channelList.RemoveFirst();
                    _logger?.LogTrace(channel + " retrieved from cache");
                    if (channel.IsOpen)
                    {
                        break;
                    }
                    else
                    {
                        CleanUpClosedChannel(channel);
                        channel = null;
                    }
                }
            }

            return channel;
        }

        private void CleanUpClosedChannel(IChannelProxy channel)
        {
            try
            {
                var target = channel.TargetChannel;
                if (target != null)
                {
                    target.Close();
                    /*
                     *  To remove it from auto-recovery if so configured,
                     *  and nack any pending confirms if PublisherCallbackChannel.
                     */
                }
            }
            catch (AlreadyClosedException)
            {
                _logger?.LogTrace(channel + " is already closed");
            }
            catch (TimeoutException e)
            {
                _logger?.LogWarning("TimeoutException closing channel " + e.Message);
            }
            catch (Exception e)
            {
                _logger?.LogDebug("Unexpected Exception closing channel " + e.Message);
            }
        }

        private LinkedList<IChannelProxy> DetermineChannelList(ChannelCachingConnectionProxy connection, bool transactional)
        {
            LinkedList<IChannelProxy> channelList;
            if (CacheMode == CachingMode.CHANNEL)
            {
                if (transactional)
                {
                    channelList = _cachedChannelsTransactional;
                }
                else
                {
                    channelList = _cachedChannelsNonTransactional;
                }
            }
            else
            {
                if (transactional)
                {
                    _allocatedConnectionTransactionalChannels.TryGetValue(connection, out channelList);
                }
                else
                {
                    _allocatedConnectionNonTransactionalChannels.TryGetValue(connection, out channelList);
                }
            }

            if (channelList == null)
            {
                throw new InvalidOperationException("No channel list for connection " + connection);
            }

            return channelList;
        }

        private IChannelProxy GetCachedChannelProxy(ChannelCachingConnectionProxy connection, LinkedList<IChannelProxy> channelList, bool transactional)
        {
            var targetChannel = CreateBareChannel(connection, transactional);
            _logger?.LogDebug("Creating cached Rabbit Channel from " + targetChannel);
            ChannelListener.OnCreate(targetChannel, transactional);
            if (_confirmType == ConfirmType.CORRELATED || IsPublisherReturns)
            {
                return new CachedPublisherCallbackChannelProxy(this, connection, targetChannel, channelList, transactional, _logger);
            }
            else
            {
                return new CachedChannelProxy(this, connection, targetChannel, channelList, transactional, _logger);
            }
        }

        private IModel CreateBareChannel(ChannelCachingConnectionProxy connection, bool transactional)
        {
            if (CacheMode == CachingMode.CHANNEL)
            {
                if (!_connection.IsOpen)
                {
                    lock (_connectionMonitor)
                    {
                        if (!_connection.IsOpen)
                        {
                            _connection.NotifyCloseIfNecessary();
                        }

                        if (!_connection.IsOpen)
                        {
                            _connection.Target = null;
                            CreateConnection();
                        }
                    }
                }

                return DoCreateBareChannel(_connection, transactional);
            }
            else if (CacheMode == CachingMode.CONNECTION)
            {
                if (!connection.IsOpen)
                {
                    lock (_connectionMonitor)
                    {
                        _allocatedConnectionNonTransactionalChannels.TryGetValue(connection, out var proxies);
                        proxies?.Clear();
                        connection.NotifyCloseIfNecessary();
                        RefreshProxyConnection(connection);
                    }
                }

                return DoCreateBareChannel(connection, transactional);
            }

            return null; // NOSONAR doCreate will throw an exception
        }

        private IModel DoCreateBareChannel(ChannelCachingConnectionProxy conn, bool transactional)
        {
            var channel = conn.CreateBareChannel(transactional);
            if (!ConfirmType.NONE.Equals(_confirmType))
            {
                try
                {
                    channel.ConfirmSelect();
                }
                catch (IOException e)
                {
                    _logger?.LogError("Could not configure the channel to receive publisher confirms", e);
                }
            }

            if ((IsPublisherConfirms || IsPublisherReturns) && !(channel is PublisherCallbackChannel))
            {
                channel = PublisherCallbackChannelFactory.CreateChannel(channel);
            }

            if (channel != null)
            {
                // channel.AddShutdownListener(this);
                channel.ModelShutdown += ShutdownCompleted;
            }

            return channel; // NOSONAR - Simple connection throws exception
        }

        private long CurrentTimeMillis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private IConnection GetConnectionFromCache()
        {
            var cachedConnection = FindIdleConnection();
            var now = CurrentTimeMillis();
            if (cachedConnection == null && CountOpenConnections() >= ConnectionLimit)
            {
                cachedConnection = WaitForConnection(now);
            }

            if (cachedConnection == null)
            {
                if (CountOpenConnections() >= ConnectionLimit
                        && CurrentTimeMillis() - now >= ChannelCheckoutTimeout)
                {
                    throw new AmqpTimeoutException("Timed out attempting to get a connection");
                }

                cachedConnection = new ChannelCachingConnectionProxy(this, CreateBareConnection(), _logger);
                _logger?.LogDebug("Adding new connection '" + cachedConnection + "'");

                _allocatedConnections.Add(cachedConnection);

                var nonTrans = new LinkedList<IChannelProxy>();
                var trans = new LinkedList<IChannelProxy>();
                _allocatedConnectionNonTransactionalChannels[cachedConnection] = nonTrans;
                _allocatedConnectionTransactionalChannels[cachedConnection] = trans;

                _channelHighWaterMarks[RuntimeHelpers.GetHashCode(nonTrans)] = new AtomicInteger();
                _channelHighWaterMarks[RuntimeHelpers.GetHashCode(trans)] = new AtomicInteger();

                _checkoutPermits[cachedConnection] = new SemaphoreSlim(ChannelCacheSize);

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
                    _idleConnections.AddLast(cachedConnection);
                }
            }
            else
            {
                _logger?.LogDebug("Obtained connection '" + cachedConnection + "' from cache");
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
                        _logger?.LogError("Exception while waiting for a connection", e);
                        throw new AmqpException("Interrupted while waiting for a connection", e);
                    }
                }
            }

            return cachedConnection;
        }

        private ChannelCachingConnectionProxy FindIdleConnection()
        {
            ChannelCachingConnectionProxy cachedConnection = null;
            var lastIdle = _idleConnections.Last?.Value;
            while (cachedConnection == null)
            {
                cachedConnection = _idleConnections.First?.Value;
                if (cachedConnection != null)
                {
                    _idleConnections.RemoveFirst();
                    if (!cachedConnection.IsOpen)
                    {
                        _logger?.LogDebug("Skipping closed connection '" + cachedConnection + "'");
                        cachedConnection.NotifyCloseIfNecessary();
                        _idleConnections.AddLast(cachedConnection);

                        if (cachedConnection == lastIdle)
                        {
                            // all of the idle connections are closed.
                            cachedConnection = _idleConnections.First?.Value;
                            if (cachedConnection != null)
                            {
                                _idleConnections.RemoveFirst();
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
            connection._closeNotified = 0;
            ConnectionListener.OnCreate(connection);
            _logger?.LogDebug("Refreshed existing connection '" + connection + "'");
        }

        private void InitCacheWaterMarks()
        {
            _channelHighWaterMarks[RuntimeHelpers.GetHashCode(_cachedChannelsNonTransactional)] = new AtomicInteger();
            _channelHighWaterMarks[RuntimeHelpers.GetHashCode(_cachedChannelsTransactional)] = new AtomicInteger();
        }
        #endregion

        #region Nested Types

        internal class CachedPublisherCallbackChannelProxy : CachedChannelProxy, IPublisherCallbackChannel
        {
            public CachedPublisherCallbackChannelProxy(
                CachingConnectionFactory factory,
                ChannelCachingConnectionProxy connection,
                IModel target,
                LinkedList<IChannelProxy> channelList,
                bool transactional,
                ILogger logger)
                : base(factory, connection, target, channelList, transactional, logger)
            {
            }

            #region IPublisherCallbackChannel

            public IModel Channel
            {
                get
                {
                    return _target;
                }
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

            public void SetAfterAckCallback(Action<IModel> callback)
            {
                PublisherCallbackChannel.SetAfterAckCallback(callback);
            }

            private IPublisherCallbackChannel PublisherCallbackChannel => (IPublisherCallbackChannel)_target;

            #endregion

            public override string ToString()
            {
                return "Cached Rabbit Channel: " + _target + ", conn: " + _theConnection;
            }

            #region Protected
            protected override void ReturnToCache()
            {
                if (_factory._active && _publisherConfirms)
                {
                    _theConnection._channelsAwaitingAcks[_target] = this;
                    ((IPublisherCallbackChannel)_target)
                            .SetAfterAckCallback(c =>
                            {
                                _theConnection._channelsAwaitingAcks.Remove(c);
                                DoReturnToCache();
                            });
                }
                else
                {
                    DoReturnToCache();
                }
            }
            #endregion
        }

#pragma warning disable S3881 // "IDisposable" should be implemented correctly
        internal class CachedChannelProxy : IChannelProxy
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
        {
            protected const int ASYNC_CLOSE_TIMEOUT = 5_000;
            protected readonly CachingConnectionFactory _factory;
            protected readonly ChannelCachingConnectionProxy _theConnection;
            protected readonly LinkedList<IChannelProxy> _channelList;
            protected readonly int _channelListIdentity;
            protected readonly object _targetMonitor = new object();
            protected readonly bool _transactional;
            protected readonly bool _confirmSelected;
            protected readonly bool _publisherConfirms;
            protected readonly ILogger _logger;
            protected IModel _target;
            protected bool _txStarted;

            public CachedChannelProxy(
                CachingConnectionFactory factory,
                ChannelCachingConnectionProxy connection,
                IModel target,
                LinkedList<IChannelProxy> channelList,
                bool transactional,
                ILogger logger)
            {
                _factory = factory;
                _theConnection = connection;
                _target = target;
                _channelList = channelList;
                _channelListIdentity = RuntimeHelpers.GetHashCode(channelList);
                _transactional = transactional;
                _confirmSelected = _factory.IsSimplePublisherConfirms;
                _publisherConfirms = _factory.IsPublisherConfirms;
                _logger = logger;
            }

            #region IChannelProxy

            public IModel TargetChannel => _target;

            public bool IsTransactional => _transactional;

            public bool IsConfirmSelected => _confirmSelected;

            #endregion

            #region IModel
            public int ChannelNumber
            {
                get
                {
                    try
                    {
                        PreInvoke();
                        return _target.ChannelNumber;
                    }
                    catch (Exception e)
                    {
                        PostException(e);
                        throw;
                    }
                }
            }

            public ShutdownEventArgs CloseReason
            {
                get
                {
                    try
                    {
                        PreInvoke();
                        return _target.CloseReason;
                    }
                    catch (Exception e)
                    {
                        PostException(e);
                        throw;
                    }
                }
            }

            public IBasicConsumer DefaultConsumer
            {
                get
                {
                    try
                    {
                        PreInvoke();
                        return _target.DefaultConsumer;
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
                        _target.DefaultConsumer = value;
                    }
                    catch (Exception e)
                    {
                        PostException(e);
                        throw;
                    }
                }
            }

            public bool IsClosed
            {
                get
                {
                    try
                    {
                        PreInvoke();
                        return _target.IsClosed;
                    }
                    catch (Exception e)
                    {
                        PostException(e);
                        throw;
                    }
                }
            }

            public bool IsOpen
            {
                get
                {
                    return _target != null && _target.IsOpen;
                }
            }

            public ulong NextPublishSeqNo
            {
                get
                {
                    try
                    {
                        PreInvoke();
                        return _target.NextPublishSeqNo;
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
                        return _target.ContinuationTimeout;
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
                        _target.ContinuationTimeout = value;
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
                        _target.BasicAcks += value;
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
                        _target.BasicAcks -= value;
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
                        _target.BasicNacks += value;
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
                        _target.BasicNacks -= value;
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
                        _target.BasicRecoverOk += value;
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
                        _target.BasicRecoverOk -= value;
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
                        _target.BasicReturn += value;
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
                        _target.BasicReturn -= value;
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
                        _target.CallbackException += value;
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
                        _target.CallbackException -= value;
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
                        _target.FlowControl += value;
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
                        _target.FlowControl -= value;
                    }
                    catch (Exception e)
                    {
                        PostException(e);
                        throw;
                    }
                }
            }

            public event EventHandler<ShutdownEventArgs> ModelShutdown
            {
                add
                {
                    try
                    {
                        PreInvoke();
                        _target.ModelShutdown += value;
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
                        _target.ModelShutdown -= value;
                    }
                    catch (Exception e)
                    {
                        PostException(e);
                        throw;
                    }
                }
            }

            public void Abort()
            {
                try
                {
                    PreInvoke();
                    _target.Abort();
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
                    _target.Abort(replyCode, replyText);
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
                    if (_target == null || !_target.IsOpen)
                    {
                        throw new InvalidOperationException("Channel closed; cannot ack/nack");
                    }

                    PreInvoke();
                    _target.BasicAck(deliveryTag, multiple);
                    if (_transactional)
                    {
                        _txStarted = true;
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
                    _target.BasicCancel(consumerTag);
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
            {
                try
                {
                    PreInvoke();
                    return _target.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer);
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public BasicGetResult BasicGet(string queue, bool autoAck)
            {
                try
                {
                    PreInvoke();
                    return _target.BasicGet(queue, autoAck);
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
                    if (_target == null || !_target.IsOpen)
                    {
                        throw new InvalidOperationException("Channel closed; cannot ack/nack");
                    }

                    PreInvoke();
                    _target.BasicNack(deliveryTag, multiple, requeue);
                    if (_transactional)
                    {
                        _txStarted = true;
                    }
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, byte[] body)
            {
                try
                {
                    PreInvoke();
                    _target.BasicPublish(exchange, routingKey, mandatory, basicProperties, body);
                    if (_transactional)
                    {
                        _txStarted = true;
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
                    _target.BasicQos(prefetchSize, prefetchCount, global);
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
                    _target.BasicRecover(requeue);
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
                    _target.BasicRecoverAsync(requeue);
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
                    if (_target == null || !_target.IsOpen)
                    {
                        throw new InvalidOperationException("Channel closed; cannot ack/nack");
                    }

                    PreInvoke();
                    _target.BasicReject(deliveryTag, requeue);
                    if (_transactional)
                    {
                        _txStarted = true;
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
                    _target.ConfirmSelect();
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
                    return _target.ConsumerCount(queue);
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public IBasicProperties CreateBasicProperties()
            {
                try
                {
                    PreInvoke();
                    return _target.CreateBasicProperties();
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public IBasicPublishBatch CreateBasicPublishBatch()
            {
                try
                {
                    PreInvoke();
                    return _target.CreateBasicPublishBatch();
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
                    _target.ExchangeBind(destination, source, routingKey, arguments);
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
                    _target.ExchangeBindNoWait(destination, source, routingKey, arguments);
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
                    _target.ExchangeDeclare(exchange, type, durable, autoDelete, arguments);
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
                    _target.ExchangeDeclareNoWait(exchange, type, durable, autoDelete, arguments);
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
                    _target.ExchangeDeclarePassive(exchange);
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
                    _target.ExchangeDelete(exchange, ifUnused);
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
                    _target.ExchangeDeleteNoWait(exchange, ifUnused);
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
                    _target.ExchangeUnbind(destination, source, routingKey, arguments);
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
                    _target.ExchangeUnbindNoWait(destination, source, routingKey, arguments);
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
                    return _target.MessageCount(queue);
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
                    _target.QueueBind(queue, exchange, routingKey, arguments);
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
                    _target.QueueBindNoWait(queue, exchange, routingKey, arguments);
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
            {
                try
                {
                    PreInvoke();
                    return _target.QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
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
                    _target.QueueDeclareNoWait(queue, durable, exclusive, autoDelete, arguments);
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }

            public QueueDeclareOk QueueDeclarePassive(string queue)
            {
                try
                {
                    PreInvoke();
                    return _target.QueueDeclarePassive(queue);
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
                    return _target.QueueDelete(queue, ifUnused, ifEmpty);
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
                    _target.QueueDeleteNoWait(queue, ifUnused, ifEmpty);
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
                    return _target.QueuePurge(queue);
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
                    _target.QueueUnbind(queue, exchange, routingKey, arguments);
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
                    _target.TxCommit();
                    if (_transactional)
                    {
                        _txStarted = false;
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
                    _target.TxRollback();
                    if (_transactional)
                    {
                        _txStarted = false;
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
                    if (!_transactional)
                    {
                        throw new InvalidOperationException("Cannot start transaction on non-transactional channel");
                    }

                    _target.TxSelect();
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
                    return _target.WaitForConfirms();
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
                    return _target.WaitForConfirms(timeout);
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
                    return _target.WaitForConfirms(timeout, out timedOut);
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
                    _target.WaitForConfirmsOrDie();
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
                    _target.WaitForConfirmsOrDie(timeout);
                }
                catch (Exception e)
                {
                    PostException(e);
                    throw;
                }
            }
            #endregion

            #region IDisposable
            public void Dispose()
            {
                // Do Nothing
            }

            #endregion

            public override string ToString()
            {
                return "Cached Rabbit Channel: " + _target + ", conn: " + _theConnection;
            }

            #region protected

            protected virtual void PostException(Exception e)
            {
                if (_target == null || !_target.IsOpen)
                {
                    // Basic re-connection logic...
                    _logger?.LogDebug("Detected closed channel on exception.  Re-initializing: " + _target);
                    _target = null;

                    lock (_targetMonitor)
                    {
                        if (_target == null)
                        {
                            _target = _factory.CreateBareChannel(_theConnection, _transactional);
                        }
                    }
                }
            }

            protected virtual void PreInvoke()
            {
                if (_target == null || !_target.IsOpen)
                {
                    if (_target is IPublisherCallbackChannel)
                    {
                        _target.Close();
                        throw new AmqpException("PublisherCallbackChannel is closed");
                    }
                    else if (_txStarted)
                    {
                        _txStarted = false;
                        throw new InvalidOperationException("Channel closed during transaction");
                    }

                    _target = null;
                }

                lock (_targetMonitor)
                {
                    if (_target == null)
                    {
                        _target = _factory.CreateBareChannel(_theConnection, _transactional);
                    }
                }
            }

            protected virtual void ReleasePermitIfNecessary()
            {
                if (_factory.ChannelCheckoutTimeout > 0)
                {
                    /*
                     *  Only release a permit if this is a normal close; if the channel is
                     *  in the list, it means we're closing a cached channel (for which a permit
                     *  has already been released).
                     */
                    lock (_channelList)
                    {
                        if (_channelList.Contains(this))
                        {
                            return;
                        }
                    }

                    if (_factory._checkoutPermits.TryGetValue(_theConnection, out var permits))
                    {
                        permits.Release();
                        _logger?.LogDebug("Released permit for '" + _theConnection + "', remaining: " + permits.CurrentCount);
                    }
                    else
                    {
                        _logger?.LogError("LEAKAGE: No permits map entry for " + _theConnection);
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
                lock (_channelList)
                {
                    // Allow for multiple close calls...
                    if (_factory._active)
                    {
                        if (!_channelList.Contains(this))
                        {
                            _logger?.LogTrace("Returning cached Channel: " + _target);
                            ReleasePermitIfNecessary();
                            _channelList.AddLast(this);
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
                                _logger?.LogError("Exception while doing PhysicalClose()", e);
                            }
                        }
                    }
                }

                // }
                // }
            }

            protected virtual void LogicalClose()
            {
                if (_target == null)
                {
                    return;
                }

                if (!_target.IsOpen)
                {
                    lock (_targetMonitor)
                    {
                        if (!_target.IsOpen)
                        {
                            if (_target is IPublisherCallbackChannel)
                            {
                                _target.Close(); // emit nacks if necessary
                            }

                            if (_channelList.Contains(this))
                            {
                                _channelList.Remove(this);
                            }
                            else
                            {
                                ReleasePermitIfNecessary();
                            }

                            _target = null;
                            return;
                        }
                    }
                }

                ReturnToCache();
            }

            protected void SetHighWaterMark()
            {
                if (_factory._channelHighWaterMarks.TryGetValue(_channelListIdentity, out var hwm))
                {
                    // No need for atomicity since we're sync'd on the channel list
                    var prev = hwm.Value;
                    var size = _channelList.Count;
                    if (size > prev)
                    {
                        hwm.Value = size;
                    }
                }
            }

            protected virtual void PhysicalClose()
            {
                _logger?.LogDebug("Closing cached Channel: " + _target);
                if (_target == null)
                {
                    return;
                }

                var asyncClose = false;
                try
                {
                    if (_factory._active && (_factory.IsPublisherConfirms || _factory.IsPublisherReturns))
                    {
                        asyncClose = true;
                        AsyncClose();
                    }
                    else
                    {
                        _target.Close();

                        // if (this.target instanceof AutorecoveringChannel) {
                        //    ClosingRecoveryListener.removeChannel((AutorecoveringChannel)this.target);
                        // }
                    }
                }
                catch (AlreadyClosedException e)
                {
                    _logger?.LogTrace(_target + " is already closed", e);
                }
                finally
                {
                    _target = null;
                    if (!asyncClose)
                    {
                        ReleasePermitIfNecessary();
                    }
                }
            }

            protected void AsyncClose()
            {
                // ExecutorService executorService = getChannelsExecutor();
                var channel = _target;

                // _factory._inFlightAsyncCloses.Add(channel);
                try
                {
                    Task.Run(() =>
                    {
                        _logger?.LogDebug("Starting AsyncClose processing");
                        try
                        {
                            if (_factory.IsPublisherConfirms)
                            {
                                channel.WaitForConfirmsOrDie(TimeSpan.FromMilliseconds(ASYNC_CLOSE_TIMEOUT));
                            }
                            else
                            {
                                Thread.Sleep(ASYNC_CLOSE_TIMEOUT);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger?.LogError("Exception in AsyncClose processing", e);
                        }
                        finally
                        {
                            try
                            {
                                channel.Close();
                            }
                            catch (Exception e)
                            {
                                _logger?.LogError("Exception in AsyncClose issued channel close", e);
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
                    _logger?.LogError("Exception while running AsyncClose processing", e);

                    // _factory._inFlightAsyncCloses.release(channel);
                }
            }

            protected virtual void DoClose()
            {
                if (_factory._active)
                {
                    lock (_channelList)
                    {
                        if (_factory._active &&
                            !RabbitUtils.IsPhysicalCloseRequired() &&
                            (_channelList.Count < _factory.ChannelCacheSize || _channelList.Contains(this)))
                        {
                            LogicalClose();
                            return;
                        }
                    }
                }

                PhysicalClose();
            }
            #endregion
        }

#pragma warning disable S3881 // "IDisposable" should be implemented correctly
        internal class ChannelCachingConnectionProxy : IConnectionProxy
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
        {
            internal readonly Dictionary<IModel, IChannelProxy> _channelsAwaitingAcks = new Dictionary<IModel, IChannelProxy>();
            internal int _closeNotified;
            private readonly CachingConnectionFactory _factory;
            private readonly ILogger _logger;

            public ChannelCachingConnectionProxy(CachingConnectionFactory factory, IConnection target, ILogger logger)
            {
                Target = target;
                _factory = factory;
                _logger = logger;
            }

            internal IConnection Target { get; set; }

            public IModel CreateChannel(bool transactional = false)
            {
                return _factory.GetChannel(this, transactional);
            }

            public IModel CreateBareChannel(bool transactional)
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
                if (_factory.CacheMode == CachingMode.CONNECTION)
                {
                    lock (_factory._connectionMonitor)
                    {
                        if (!_factory._idleConnections.Contains(this))
                        {
                            if (!IsOpen || CountOpenIdleConnections() >= _factory.ConnectionCacheSize)
                            {
                                _logger?.LogDebug("Completely closing connection '" + this + "'");
                                Dispose();
                            }

                            _logger?.LogDebug("Returning connection '" + this + "' to cache");

                            _factory._idleConnections.AddLast(this);
                            var idleConnectionsSize = _factory._idleConnections.Count;
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
                if (_factory.CacheMode == CachingMode.CHANNEL)
                {
                    _factory.Reset(_factory._cachedChannelsNonTransactional, _factory._cachedChannelsTransactional, _channelsAwaitingAcks);
                }
                else
                {
                    _factory._allocatedConnectionNonTransactionalChannels.TryGetValue(this, out var nonTrans);
                    _factory._allocatedConnectionTransactionalChannels.TryGetValue(this, out var trans);
                    _factory.Reset(nonTrans, trans, _channelsAwaitingAcks);
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
                if (Interlocked.CompareExchange(ref _closeNotified, 1, 0) == 0)
                {
                    _factory.ConnectionListener.OnClose(this);
                }
            }

            public bool IsOpen
            {
                get { return Target != null && Target.IsOpen; }
            }

            public IConnection TargetConnection => Target;

            public RabbitMQ.Client.IConnection Connection
            {
                get { return Target.Connection; }
            }

            public int LocalPort
            {
                get
                {
                    var target = Target;
                    if (target != null)
                    {
                        return target.LocalPort;
                    }

                    return 0;
                }
            }

            public override string ToString()
            {
                return "Proxy@" + RuntimeHelpers.GetHashCode(this) + " " + ((_factory.CacheMode == CachingMode.CHANNEL) ? "Shared " : "Dedicated ") + "Rabbit Connection: " + Target;
            }

            private int CountOpenIdleConnections()
            {
                var n = 0;
                foreach (var proxy in _factory._idleConnections)
                {
                    if (proxy.IsOpen)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        private class DefaultChannelCloseLogger : IConditionalExceptionLogger
        {
            public void Log(ILogger logger, string message, object cause)
            {
                logger?.LogError("Unexpected invocation of " + GetType() + ", with message: " + message, cause);
            }
        }
        #endregion
    }
}
