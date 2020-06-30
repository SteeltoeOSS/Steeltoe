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
using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Config;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class SingleConnectionFactory : AbstractConnectionFactory
    {
        public const string DEFAULT_SERVICE_NAME = "scFactory";

        private readonly object _connectionMonitor = new object();

        public SingleConnectionFactory(ILoggerFactory loggerFactory = null)
            : this((string)null, loggerFactory)
        {
        }

        public SingleConnectionFactory(int port, ILoggerFactory loggerFactory = null)
            : this(null, port, loggerFactory)
        {
        }

        public SingleConnectionFactory(string hostname, ILoggerFactory loggerFactory = null)
            : this(hostname, RabbitOptions.DEFAULT_PORT, loggerFactory)
        {
        }

        public SingleConnectionFactory(string hostname, int port, ILoggerFactory loggerFactory = null)
            : base(new RabbitMQ.Client.ConnectionFactory(), loggerFactory)
        {
            if (string.IsNullOrEmpty(hostname))
            {
                hostname = GetDefaultHostName();
            }

            Host = hostname;
            Port = port;
            ServiceName = DEFAULT_SERVICE_NAME;
        }

        public SingleConnectionFactory(Uri uri, ILoggerFactory loggerFactory = null)
            : base(new RabbitMQ.Client.ConnectionFactory(), loggerFactory)
        {
            Uri = uri;
            ServiceName = DEFAULT_SERVICE_NAME;
        }

        public SingleConnectionFactory(RabbitMQ.Client.IConnectionFactory rabbitConnectionFactory, ILoggerFactory loggerFactory = null)
            : base(rabbitConnectionFactory, loggerFactory)
        {
            ServiceName = DEFAULT_SERVICE_NAME;
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

        public override void AddConnectionListener(IConnectionListener listener)
        {
            base.AddConnectionListener(listener);

            // If the connection is already alive we assume that the new listener wants to be notified
            if (Connection != null)
            {
                listener.OnCreate(Connection);
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
            return "SingleConnectionFactory [host=" + Host + ", port=" + Port + "]";
        }

        protected IConnection DoCreateConnection()
        {
            var connection = CreateBareConnection();
            return connection;
        }

        public class SharedConnectionProxy : IConnectionProxy
        {
            private readonly ILogger _logger;

            private readonly SingleConnectionFactory _factory;

            public IConnection Target { get; set; }

            public SharedConnectionProxy(SingleConnectionFactory factory, IConnection target, ILogger logger = null)
            {
                _logger = logger;
                _factory = factory;
                Target = target;
            }

            public IModel CreateChannel(bool transactional)
            {
                if (!IsOpen)
                {
                    lock (this)
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

            public RabbitMQ.Client.IConnection Connection
            {
                get
                {
                    var asSimple = Target as SimpleConnection;
                    return asSimple.Connection;
                }
            }

            public override int GetHashCode()
            {
                return 31 + ((Target == null) ? 0 : Target.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (GetType() != obj.GetType())
                {
                    return false;
                }

                var other = (SharedConnectionProxy)obj;
                if (Target == null)
                {
                    if (other.Target != null)
                    {
                        return false;
                    }
                }
                else if (!Target.Equals(other.Target))
                {
                    return false;
                }

                return true;
            }

            public override string ToString()
            {
                return "Shared Rabbit Connection: " + Target;
            }

            public void Dispose()
            {
                Destroy();
            }
        }
    }
}
