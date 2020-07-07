// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using static Steeltoe.Messaging.Rabbit.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    [Trait("Category", "Integration")]
    public class CachingConnectionFactoryIntegrationTests : IDisposable
    {
        public const string CF_INTEGRATION_TEST_QUEUE = "cfIntegrationTest";
        private const string CF_INTEGRATION_CONNECTION_NAME = "cfIntegrationTestConnectionName";

        private readonly CachingConnectionFactory connectionFactory;

        public CachingConnectionFactoryIntegrationTests()
        {
            connectionFactory = new CachingConnectionFactory("localhost")
            {
                ServiceName = CF_INTEGRATION_CONNECTION_NAME
            };
        }

        public void Dispose()
        {
            connectionFactory.Destroy();
        }

        [Fact]
        public void TestCachedConnections()
        {
            connectionFactory.CacheMode = CachingMode.CONNECTION;
            connectionFactory.ConnectionCacheSize = 5;
            var connections = new List<IConnection>
            {
                connectionFactory.CreateConnection(),
                connectionFactory.CreateConnection()
            };
            Assert.NotSame(connections[0], connections[1]);
            connections.Add(connectionFactory.CreateConnection());
            connections.Add(connectionFactory.CreateConnection());
            connections.Add(connectionFactory.CreateConnection());
            connections.Add(connectionFactory.CreateConnection());
            var allocatedConnections = connectionFactory._allocatedConnections;
            Assert.Equal(6, allocatedConnections.Count);
            foreach (var c in allocatedConnections)
            {
                c.Close();
            }

            var idleConnections = connectionFactory._idleConnections;
            Assert.Equal(6, idleConnections.Count);
            connections.Clear();
            connections.Add(connectionFactory.CreateConnection());
            connections.Add(connectionFactory.CreateConnection());
            allocatedConnections = connectionFactory._allocatedConnections;
            Assert.Equal(6, allocatedConnections.Count);
            idleConnections = connectionFactory._idleConnections;
            Assert.Equal(4, idleConnections.Count);
            foreach (var c in connections)
            {
                c.Close();
            }
        }

        [Fact]
        public void TestCachedConnectionsChannelLimit()
        {
            connectionFactory.CacheMode = CachingMode.CONNECTION;
            connectionFactory.ConnectionCacheSize = 2;
            connectionFactory.ChannelCacheSize = 1;
            connectionFactory.ChannelCheckoutTimeout = 10;
            var connections = new List<IConnection>
            {
                connectionFactory.CreateConnection(),
                connectionFactory.CreateConnection()
            };
            var channels = new List<IModel>
            {
                connections[0].CreateChannel(false)
            };
            try
            {
                channels.Add(connections[0].CreateChannel(false));
                throw new Exception("Exception expected");
            }
            catch (RabbitTimeoutException)
            {
                // Ignore
            }

            channels.Add(connections[1].CreateChannel(false));
            try
            {
                channels.Add(connections[1].CreateChannel(false));
                throw new Exception("Exception expected");
            }
            catch (RabbitTimeoutException)
            {
                // Ignore
            }

            channels[0].Close();
            channels[1].Close();

            channels.Add(connections[0].CreateChannel(false));
            channels.Add(connections[1].CreateChannel(false));

            Assert.Same(channels[2], channels[0]);
            Assert.Same(channels[3], channels[1]);
            channels[2].Close();
            channels[3].Close();
            foreach (var c in connections)
            {
                c.Close();
            }
        }

        [Fact]
        public void TestCachedConnectionsAndChannels()
        {
            connectionFactory.CacheMode = CachingMode.CONNECTION;
            connectionFactory.ConnectionCacheSize = 1;
            connectionFactory.ChannelCacheSize = 3;
            var connections = new List<IConnection>
            {
                connectionFactory.CreateConnection(),
                connectionFactory.CreateConnection()
            };
            var allocatedConnections = connectionFactory._allocatedConnections;
            Assert.Equal(2, allocatedConnections.Count);
            Assert.NotSame(connections[0], connections[1]);
            var channels = new List<IModel>();
            for (var i = 0; i < 5; i++)
            {
                channels.Add(connections[0].CreateChannel(false));
                channels.Add(connections[1].CreateChannel(false));
                channels.Add(connections[0].CreateChannel(true));
                channels.Add(connections[1].CreateChannel(true));
            }

            var cachedChannels = connectionFactory._allocatedConnectionNonTransactionalChannels;
            Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[0]]);
            Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);

            var cachedTxChannels = connectionFactory._allocatedConnectionTransactionalChannels;
            Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]]);
            Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]]);
            foreach (var c in channels)
            {
                c.Close();
            }

            Assert.Equal(3, cachedChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
            Assert.Equal(3, cachedChannels[(ChannelCachingConnectionProxy)connections[1]].Count);
            Assert.Equal(3, cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
            Assert.Equal(3, cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]].Count);
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(channels[i * 4], connections[0].CreateChannel(false));
                Assert.Equal(channels[(i * 4) + 1], connections[1].CreateChannel(false));
                Assert.Equal(channels[(i * 4) + 2], connections[0].CreateChannel(true));
                Assert.Equal(channels[(i * 4) + 3], connections[1].CreateChannel(true));
            }

            cachedChannels = connectionFactory._allocatedConnectionNonTransactionalChannels;
            Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[0]]);
            Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);

            cachedTxChannels = connectionFactory._allocatedConnectionTransactionalChannels;
            Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]]);
            Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]]);
            foreach (var c in channels)
            {
                c.Close();
            }

            foreach (var c in connections)
            {
                c.Close();
            }

            Assert.Equal(3, cachedChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
            Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);
            Assert.Equal(3, cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
            Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]]);

            allocatedConnections = connectionFactory._allocatedConnections;
            Assert.Equal(2, allocatedConnections.Count);
            var props = connectionFactory.GetCacheProperties();
            Assert.Equal(1, props["openConnections"]);

            var connection = connectionFactory.CreateConnection();
            var rabbitConnection = connection.Connection;
            rabbitConnection.Close();

            var channel = connection.CreateChannel(false);
            allocatedConnections = connectionFactory._allocatedConnections;
            Assert.Equal(2, allocatedConnections.Count);
            props = connectionFactory.GetCacheProperties();
            Assert.Equal(1, props["openConnections"]);

            channel.Close();
            connection.Close();

            allocatedConnections = connectionFactory._allocatedConnections;
            Assert.Equal(2, allocatedConnections.Count);
            props = connectionFactory.GetCacheProperties();
            Assert.Equal(1, props["openConnections"]);
        }

        [Fact]
        public void TestSendAndReceiveFromVolatileQueue()
        {
            var template = new RabbitTemplate(connectionFactory);
            var admin = new RabbitAdmin(connectionFactory);
            var queue = admin.DeclareQueue();
            template.ConvertAndSend(queue.QueueName, "message");
            var result = template.ReceiveAndConvert<string>(queue.QueueName);
            Assert.Equal("message", result);
        }

        [Fact]
        public void TestReceiveFromNonExistentVirtualHost()
        {
            connectionFactory.VirtualHost = "non-existent";
            var template = new RabbitTemplate(connectionFactory);
            Assert.Throws<RabbitConnectException>(() => template.ReceiveAndConvert<string>("foo"));
        }

        [Fact]
        public void TestSendAndReceiveFromVolatileQueueAfterImplicitRemoval()
        {
            var template = new RabbitTemplate(connectionFactory);
            var admin = new RabbitAdmin(connectionFactory);
            var queue = admin.DeclareQueue();
            template.ConvertAndSend(queue.QueueName, "message");
            connectionFactory.ResetConnection();
            Assert.Throws<RabbitIOException>(() => template.ReceiveAndConvert<string>(queue.QueueName));
        }

        [Fact]
        public void TestMixTransactionalAndNonTransactional()
        {
            var template1 = new RabbitTemplate(connectionFactory);
            var template2 = new RabbitTemplate(connectionFactory);
            template1.IsChannelTransacted = true;

            var admin = new RabbitAdmin(connectionFactory);
            var queue = admin.DeclareQueue();
            template1.ConvertAndSend(queue.QueueName, "message");
            var result = template2.ReceiveAndConvert<string>(queue.QueueName);
            Assert.Equal("message", result);

            Assert.Throws<RabbitIOException>(() => template2.Execute<object>((c) =>
            {
                c.TxRollback();
                return null;
            }));
        }

        [Fact]
        public void TestHardErrorAndReconnectNoAuto()
        {
            var template = new RabbitTemplate(connectionFactory);
            var admin = new RabbitAdmin(connectionFactory);
            var queue = new Config.Queue(CF_INTEGRATION_TEST_QUEUE);
            admin.DeclareQueue(queue);
            var route = queue.QueueName;
            var latch = new CountdownEvent(1);
            try
            {
                template.Execute((channel) =>
                {
                    channel.ModelShutdown += (sender, args) =>
                    {
                        latch.Signal();
                        throw new ShutdownSignalException(args);
                    };
                    var tag = channel.BasicConsume(route, false, new DefaultBasicConsumer(channel));
                    var result = channel.BasicConsume(route, false, tag, new DefaultBasicConsumer(channel));
                    throw new Exception("Expected Exception, got: " + result);
                });
                throw new Exception("Expected AmqpIOException");
            }
            catch (RabbitIOException)
            {
            }

            template.ConvertAndSend(route, "message");
            Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
            var result = template.ReceiveAndConvert<string>(route);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(route);
            Assert.Null(result);
        }

        [Fact]
        public void TestConnectionName()
        {
            var connection = connectionFactory.CreateConnection() as ChannelCachingConnectionProxy;
            var rabbitConnection = connection.Target.Connection;
            Assert.StartsWith(CF_INTEGRATION_CONNECTION_NAME, rabbitConnection.ClientProvidedName);
        }

        [Fact]
        public void TestDestroy()
        {
            var connection1 = connectionFactory.CreateConnection();
            connectionFactory.Destroy();
            var connection2 = connectionFactory.CreateConnection();
            Assert.Same(connection1, connection2);

            connectionFactory.Dispose();
            Assert.Throws<RabbitApplicationContextClosedException>(() => connectionFactory.CreateConnection());
        }

        [Fact]
        public void TestChannelMax()
        {
            connectionFactory.RabbitConnectionFactory.RequestedChannelMax = 1;
            var connection = connectionFactory.CreateConnection();
            connection.CreateChannel(true);
            Assert.Throws<RabbitResourceNotAvailableException>(() => connection.CreateChannel(false));
        }
    }
}
