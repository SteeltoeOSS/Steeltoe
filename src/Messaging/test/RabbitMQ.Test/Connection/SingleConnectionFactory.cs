// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Config;
using System;
using System.Collections.Generic;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class SingleConnectionFactory : AbstractConnectionFactory
{
    public const string DefaultServiceName = "scFactory";

    private readonly object _connectionMonitor = new ();

    public SingleConnectionFactory(ILoggerFactory loggerFactory = null)
        : this((string)null, loggerFactory)
    {
    }

    public SingleConnectionFactory(int port, ILoggerFactory loggerFactory = null)
        : this(null, port, loggerFactory)
    {
    }

    public SingleConnectionFactory(string hostname, ILoggerFactory loggerFactory = null)
        : this(hostname, RabbitOptions.DefaultPort, loggerFactory)
    {
    }

    public SingleConnectionFactory(string hostname, int port, ILoggerFactory loggerFactory = null)
        : base(new RC.ConnectionFactory(), loggerFactory)
    {
        if (string.IsNullOrEmpty(hostname))
        {
            hostname = GetDefaultHostName();
        }

        Host = hostname;
        Port = port;
        ServiceName = DefaultServiceName;
    }

    public SingleConnectionFactory(Uri uri, ILoggerFactory loggerFactory = null)
        : base(new RC.ConnectionFactory(), loggerFactory)
    {
        Uri = uri;
        ServiceName = DefaultServiceName;
    }

    public SingleConnectionFactory(RC.IConnectionFactory rabbitConnectionFactory, ILoggerFactory loggerFactory = null)
        : base(rabbitConnectionFactory, loggerFactory)
    {
        ServiceName = DefaultServiceName;
    }

    public SharedConnectionProxy Connection { get; private set; }

    public override void SetConnectionListeners(List<IConnectionListener> listeners)
    {
        base.SetConnectionListeners(listeners);

        // If the connection is already alive we assume that the new listeners want to be notified
        if (Connection != null)
        {
            ConnectionListener.OnCreate(Connection);
        }
    }

    public override void AddConnectionListener(IConnectionListener connectionListener)
    {
        base.AddConnectionListener(connectionListener);

        // If the connection is already alive we assume that the new listener wants to be notified
        if (Connection != null)
        {
            connectionListener.OnCreate(Connection);
        }
    }

    public override IConnection CreateConnection()
    {
        lock (_connectionMonitor)
        {
            if (Connection == null)
            {
                var target = DoCreateConnection();
                Connection = new SharedConnectionProxy(this, target);

                // invoke the listener *after* this.connection is assigned
                ConnectionListener.OnCreate(target);
            }
        }

        return Connection;
    }

    public override void Destroy()
    {
        lock (_connectionMonitor)
        {
            if (Connection != null)
            {
                Connection.Destroy();
                Connection = null;
            }
        }
    }

    public override string ToString()
    {
        return $"SingleConnectionFactory [host={Host}, port={Port}]";
    }

    protected IConnection DoCreateConnection()
    {
        var connection = CreateBareConnection();
        return connection;
    }

    public sealed class SharedConnectionProxy : IConnectionProxy
    {
        private readonly ILogger _logger;
        private readonly SingleConnectionFactory _factory;
        private readonly object _lock = new ();

        public IConnection Target { get; set; }

        public SharedConnectionProxy(SingleConnectionFactory factory, IConnection target, ILogger logger = null)
        {
            _logger = logger;
            _factory = factory;
            Target = target;
        }

        public RC.IModel CreateChannel(bool transactional = false)
        {
            if (!IsOpen)
            {
                lock (_lock)
                {
                    if (!IsOpen)
                    {
                        _logger?.LogDebug("Detected closed connection. Opening a new one before creating Channel.");
                        Target = _factory.CreateBareConnection();
                        _factory.ConnectionListener.OnCreate(Target);
                    }
                }
            }

            var channel = Target.CreateChannel(transactional);
            _factory.ChannelListener.OnCreate(channel, transactional);
            return channel;
        }

        public void AddBlockedListener(IBlockedListener listener)
        {
            Target.AddBlockedListener(listener);
        }

        public bool RemoveBlockedListener(IBlockedListener listener)
        {
            return Target.RemoveBlockedListener(listener);
        }

        public void Close()
        {
        }

        public void Destroy()
        {
            if (Target != null)
            {
                _factory.ConnectionListener.OnClose(Target);
                RabbitUtils.CloseConnection(Target);
            }

            Target = null;
        }

        public bool IsOpen
        {
            get { return Target != null && Target.IsOpen; }
        }

        public IConnection TargetConnection
        {
            get { return Target; }
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

        public RC.IConnection Connection
        {
            get
            {
                var asSimple = Target as SimpleConnection;
                return asSimple.Connection;
            }
        }

        public override int GetHashCode()
        {
            return Target?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not SharedConnectionProxy other || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals(Target, other.Target);
        }

        public override string ToString()
        {
            return $"Shared Rabbit Connection: {Target}";
        }

        public void Dispose()
        {
            Destroy();
        }
    }
}
