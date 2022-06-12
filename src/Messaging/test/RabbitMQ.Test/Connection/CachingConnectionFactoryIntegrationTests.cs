// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

[Trait("Category", "Integration")]
public sealed class CachingConnectionFactoryIntegrationTests : IDisposable
{
    public const string CF_INTEGRATION_TEST_QUEUE = "cfIntegrationTest";
    private const string CF_INTEGRATION_CONNECTION_NAME = "cfIntegrationTestConnectionName";

    private readonly CachingConnectionFactory _connectionFactory;

    public CachingConnectionFactoryIntegrationTests()
    {
        _connectionFactory = new CachingConnectionFactory("localhost")
        {
            ServiceName = CF_INTEGRATION_CONNECTION_NAME
        };
    }

    public void Dispose()
    {
        _connectionFactory.Destroy();
    }

    [Fact]
    public void TestCachedConnections()
    {
        _connectionFactory.CacheMode = CachingMode.CONNECTION;
        _connectionFactory.ConnectionCacheSize = 5;
        var connections = new List<IConnection>
        {
            _connectionFactory.CreateConnection(),
            _connectionFactory.CreateConnection()
        };
        Assert.NotSame(connections[0], connections[1]);
        connections.Add(_connectionFactory.CreateConnection());
        connections.Add(_connectionFactory.CreateConnection());
        connections.Add(_connectionFactory.CreateConnection());
        connections.Add(_connectionFactory.CreateConnection());
        var allocatedConnections = _connectionFactory._allocatedConnections;
        Assert.Equal(6, allocatedConnections.Count);
        foreach (var c in allocatedConnections)
        {
            c.Close();
        }

        var idleConnections = _connectionFactory._idleConnections;
        Assert.Equal(6, idleConnections.Count);
        connections.Clear();
        connections.Add(_connectionFactory.CreateConnection());
        connections.Add(_connectionFactory.CreateConnection());
        allocatedConnections = _connectionFactory._allocatedConnections;
        Assert.Equal(6, allocatedConnections.Count);
        idleConnections = _connectionFactory._idleConnections;
        Assert.Equal(4, idleConnections.Count);
        foreach (var c in connections)
        {
            c.Close();
        }
    }

    [Fact]
    public void TestCachedConnectionsChannelLimit()
    {
        _connectionFactory.CacheMode = CachingMode.CONNECTION;
        _connectionFactory.ConnectionCacheSize = 2;
        _connectionFactory.ChannelCacheSize = 1;
        _connectionFactory.ChannelCheckoutTimeout = 10;
        var connections = new List<IConnection>
        {
            _connectionFactory.CreateConnection(),
            _connectionFactory.CreateConnection()
        };
        var channels = new List<RC.IModel>
        {
            connections[0].CreateChannel()
        };
        try
        {
            channels.Add(connections[0].CreateChannel());
            throw new Exception("Exception expected");
        }
        catch (RabbitTimeoutException)
        {
            // Ignore
        }

        channels.Add(connections[1].CreateChannel());
        try
        {
            channels.Add(connections[1].CreateChannel());
            throw new Exception("Exception expected");
        }
        catch (RabbitTimeoutException)
        {
            // Ignore
        }

        channels[0].Close();
        channels[1].Close();

        channels.Add(connections[0].CreateChannel());
        channels.Add(connections[1].CreateChannel());

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
        _connectionFactory.CacheMode = CachingMode.CONNECTION;
        _connectionFactory.ConnectionCacheSize = 1;
        _connectionFactory.ChannelCacheSize = 3;
        var connections = new List<IConnection>
        {
            _connectionFactory.CreateConnection(),
            _connectionFactory.CreateConnection()
        };
        var allocatedConnections = _connectionFactory._allocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        Assert.NotSame(connections[0], connections[1]);
        var channels = new List<RC.IModel>();
        for (var i = 0; i < 5; i++)
        {
            channels.Add(connections[0].CreateChannel());
            channels.Add(connections[1].CreateChannel());
            channels.Add(connections[0].CreateChannel(true));
            channels.Add(connections[1].CreateChannel(true));
        }

        var cachedChannels = _connectionFactory._allocatedConnectionNonTransactionalChannels;
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[0]]);
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);

        var cachedTxChannels = _connectionFactory._allocatedConnectionTransactionalChannels;
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
            Assert.Equal(channels[i * 4], connections[0].CreateChannel());
            Assert.Equal(channels[(i * 4) + 1], connections[1].CreateChannel());
            Assert.Equal(channels[(i * 4) + 2], connections[0].CreateChannel(true));
            Assert.Equal(channels[(i * 4) + 3], connections[1].CreateChannel(true));
        }

        cachedChannels = _connectionFactory._allocatedConnectionNonTransactionalChannels;
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[0]]);
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);

        cachedTxChannels = _connectionFactory._allocatedConnectionTransactionalChannels;
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

        allocatedConnections = _connectionFactory._allocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        var props = _connectionFactory.GetCacheProperties();
        Assert.Equal(1, props["openConnections"]);

        var connection = _connectionFactory.CreateConnection();
        var rabbitConnection = connection.Connection;
        rabbitConnection.Close();

        var channel = connection.CreateChannel();
        allocatedConnections = _connectionFactory._allocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        props = _connectionFactory.GetCacheProperties();
        Assert.Equal(1, props["openConnections"]);

        channel.Close();
        connection.Close();

        allocatedConnections = _connectionFactory._allocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        props = _connectionFactory.GetCacheProperties();
        Assert.Equal(1, props["openConnections"]);
    }

    [Fact]
    public void TestSendAndReceiveFromVolatileQueue()
    {
        var template = new RabbitTemplate(_connectionFactory);
        var admin = new RabbitAdmin(_connectionFactory);
        var queue = admin.DeclareQueue();
        template.ConvertAndSend(queue.QueueName, "message");
        var result = template.ReceiveAndConvert<string>(queue.QueueName);
        Assert.Equal("message", result);
    }

    [Fact]
    public void TestReceiveFromNonExistentVirtualHost()
    {
        _connectionFactory.VirtualHost = "non-existent";
        var template = new RabbitTemplate(_connectionFactory);
        Assert.Throws<RabbitConnectException>(() => template.ReceiveAndConvert<string>("foo"));
    }

    [Fact]
    public void TestSendAndReceiveFromVolatileQueueAfterImplicitRemoval()
    {
        var template = new RabbitTemplate(_connectionFactory);
        var admin = new RabbitAdmin(_connectionFactory);
        var queue = admin.DeclareQueue();
        template.ConvertAndSend(queue.QueueName, "message");
        _connectionFactory.ResetConnection();
        Assert.Throws<RabbitIOException>(() => template.ReceiveAndConvert<string>(queue.QueueName));
    }

    [Fact]
    public void TestMixTransactionalAndNonTransactional()
    {
        var template1 = new RabbitTemplate(_connectionFactory);
        var template2 = new RabbitTemplate(_connectionFactory);
        template1.IsChannelTransacted = true;

        var admin = new RabbitAdmin(_connectionFactory);
        var queue = admin.DeclareQueue();
        template1.ConvertAndSend(queue.QueueName, "message");
        var result = template2.ReceiveAndConvert<string>(queue.QueueName);
        Assert.Equal("message", result);

        Assert.Throws<RabbitIOException>(() => template2.Execute<object>(c =>
        {
            c.TxRollback();
            return null;
        }));
    }

    [Fact]
    public void TestHardErrorAndReconnectNoAuto()
    {
        var template = new RabbitTemplate(_connectionFactory);
        var admin = new RabbitAdmin(_connectionFactory);
        var queue = new Config.Queue(CF_INTEGRATION_TEST_QUEUE);
        admin.DeclareQueue(queue);
        var route = queue.QueueName;
        var latch = new CountdownEvent(1);
        try
        {
            template.Execute(channel =>
            {
                channel.ModelShutdown += (_, args) =>
                {
                    latch.Signal();
                    throw new ShutdownSignalException(args);
                };
                var tag = RC.IModelExensions.BasicConsume(channel, route, false, new RC.DefaultBasicConsumer(channel));
                var result = RC.IModelExensions.BasicConsume(channel, route, false, tag, new RC.DefaultBasicConsumer(channel));
                throw new Exception($"Expected Exception, got: {result}");
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
        admin.DeleteQueue(CF_INTEGRATION_TEST_QUEUE);
    }

    [Fact]
    public void TestConnectionName()
    {
        var connection = _connectionFactory.CreateConnection() as ChannelCachingConnectionProxy;
        var rabbitConnection = connection.Target.Connection;
        Assert.StartsWith(CF_INTEGRATION_CONNECTION_NAME, rabbitConnection.ClientProvidedName);
    }

    [Fact]
    public void TestDestroy()
    {
        var connection1 = _connectionFactory.CreateConnection();
        _connectionFactory.Destroy();
        var connection2 = _connectionFactory.CreateConnection();
        Assert.Same(connection1, connection2);

        _connectionFactory.Dispose();
        Assert.Throws<RabbitApplicationContextClosedException>(() => _connectionFactory.CreateConnection());
    }

    [Fact]
    public void TestChannelMax()
    {
        _connectionFactory.RabbitConnectionFactory.RequestedChannelMax = 1;
        var connection = _connectionFactory.CreateConnection();
        connection.CreateChannel(true);
        Assert.Throws<RabbitResourceNotAvailableException>(() => connection.CreateChannel());
    }
}
