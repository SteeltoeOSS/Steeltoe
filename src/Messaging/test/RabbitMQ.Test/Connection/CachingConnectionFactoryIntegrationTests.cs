// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

[Trait("Category", "Integration")]
public sealed class CachingConnectionFactoryIntegrationTests : IDisposable
{
    private const string CFIntegrationConnectionName = "cfIntegrationTestConnectionName";
    public const string CFIntegrationTestQueue = "cfIntegrationTest";

    private readonly CachingConnectionFactory _connectionFactory;

    public CachingConnectionFactoryIntegrationTests()
    {
        _connectionFactory = new CachingConnectionFactory("localhost")
        {
            ServiceName = CFIntegrationConnectionName
        };
    }

    [Fact]
    public void TestCachedConnections()
    {
        _connectionFactory.CacheMode = CachingMode.Connection;
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
        HashSet<ChannelCachingConnectionProxy> allocatedConnections = _connectionFactory.AllocatedConnections;
        Assert.Equal(6, allocatedConnections.Count);

        foreach (ChannelCachingConnectionProxy c in allocatedConnections)
        {
            c.Close();
        }

        LinkedList<ChannelCachingConnectionProxy> idleConnections = _connectionFactory.IdleConnections;
        Assert.Equal(6, idleConnections.Count);
        connections.Clear();
        connections.Add(_connectionFactory.CreateConnection());
        connections.Add(_connectionFactory.CreateConnection());
        allocatedConnections = _connectionFactory.AllocatedConnections;
        Assert.Equal(6, allocatedConnections.Count);
        idleConnections = _connectionFactory.IdleConnections;
        Assert.Equal(4, idleConnections.Count);

        foreach (IConnection c in connections)
        {
            c.Close();
        }
    }

    [Fact]
    public void TestCachedConnectionsChannelLimit()
    {
        _connectionFactory.CacheMode = CachingMode.Connection;
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

        foreach (IConnection c in connections)
        {
            c.Close();
        }
    }

    [Fact]
    public void TestCachedConnectionsAndChannels()
    {
        _connectionFactory.CacheMode = CachingMode.Connection;
        _connectionFactory.ConnectionCacheSize = 1;
        _connectionFactory.ChannelCacheSize = 3;

        var connections = new List<IConnection>
        {
            _connectionFactory.CreateConnection(),
            _connectionFactory.CreateConnection()
        };

        HashSet<ChannelCachingConnectionProxy> allocatedConnections = _connectionFactory.AllocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        Assert.NotSame(connections[0], connections[1]);
        var channels = new List<RC.IModel>();

        for (int i = 0; i < 5; i++)
        {
            channels.Add(connections[0].CreateChannel());
            channels.Add(connections[1].CreateChannel());
            channels.Add(connections[0].CreateChannel(true));
            channels.Add(connections[1].CreateChannel(true));
        }

        Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> cachedChannels = _connectionFactory.AllocatedConnectionNonTransactionalChannels;
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[0]]);
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);

        Dictionary<ChannelCachingConnectionProxy, LinkedList<IChannelProxy>> cachedTxChannels = _connectionFactory.AllocatedConnectionTransactionalChannels;
        Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]]);
        Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]]);

        foreach (RC.IModel c in channels)
        {
            c.Close();
        }

        Assert.Equal(3, cachedChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
        Assert.Equal(3, cachedChannels[(ChannelCachingConnectionProxy)connections[1]].Count);
        Assert.Equal(3, cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
        Assert.Equal(3, cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]].Count);

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(channels[i * 4], connections[0].CreateChannel());
            Assert.Equal(channels[i * 4 + 1], connections[1].CreateChannel());
            Assert.Equal(channels[i * 4 + 2], connections[0].CreateChannel(true));
            Assert.Equal(channels[i * 4 + 3], connections[1].CreateChannel(true));
        }

        cachedChannels = _connectionFactory.AllocatedConnectionNonTransactionalChannels;
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[0]]);
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);

        cachedTxChannels = _connectionFactory.AllocatedConnectionTransactionalChannels;
        Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]]);
        Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]]);

        foreach (RC.IModel c in channels)
        {
            c.Close();
        }

        foreach (IConnection c in connections)
        {
            c.Close();
        }

        Assert.Equal(3, cachedChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
        Assert.Empty(cachedChannels[(ChannelCachingConnectionProxy)connections[1]]);
        Assert.Equal(3, cachedTxChannels[(ChannelCachingConnectionProxy)connections[0]].Count);
        Assert.Empty(cachedTxChannels[(ChannelCachingConnectionProxy)connections[1]]);

        allocatedConnections = _connectionFactory.AllocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        IDictionary<string, object> props = _connectionFactory.GetCacheProperties();
        Assert.Equal(1, props["openConnections"]);

        IConnection connection = _connectionFactory.CreateConnection();
        RC.IConnection rabbitConnection = connection.Connection;
        rabbitConnection.Close();

        RC.IModel channel = connection.CreateChannel();
        allocatedConnections = _connectionFactory.AllocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        props = _connectionFactory.GetCacheProperties();
        Assert.Equal(1, props["openConnections"]);

        channel.Close();
        connection.Close();

        allocatedConnections = _connectionFactory.AllocatedConnections;
        Assert.Equal(2, allocatedConnections.Count);
        props = _connectionFactory.GetCacheProperties();
        Assert.Equal(1, props["openConnections"]);
    }

    [Fact]
    public void TestSendAndReceiveFromVolatileQueue()
    {
        var template = new RabbitTemplate(_connectionFactory);
        var admin = new RabbitAdmin(_connectionFactory);
        IQueue queue = admin.DeclareQueue();
        template.ConvertAndSend(queue.QueueName, "message");
        string result = template.ReceiveAndConvert<string>(queue.QueueName);
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
        IQueue queue = admin.DeclareQueue();
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
        IQueue queue = admin.DeclareQueue();
        template1.ConvertAndSend(queue.QueueName, "message");
        string result = template2.ReceiveAndConvert<string>(queue.QueueName);
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
        var queue = new Queue(CFIntegrationTestQueue);
        admin.DeclareQueue(queue);
        string route = queue.QueueName;
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

                string tag = RC.IModelExensions.BasicConsume(channel, route, false, new RC.DefaultBasicConsumer(channel));
                string result = RC.IModelExensions.BasicConsume(channel, route, false, tag, new RC.DefaultBasicConsumer(channel));
                throw new Exception($"Expected Exception, got: {result}");
            });

            throw new Exception("Expected AmqpIOException");
        }
        catch (RabbitIOException)
        {
            // Intentionally left empty.
        }

        template.ConvertAndSend(route, "message");
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
        string result = template.ReceiveAndConvert<string>(route);
        Assert.Equal("message", result);
        result = template.ReceiveAndConvert<string>(route);
        Assert.Null(result);
        admin.DeleteQueue(CFIntegrationTestQueue);
    }

    [Fact]
    public void TestConnectionName()
    {
        var connection = _connectionFactory.CreateConnection() as ChannelCachingConnectionProxy;
        RC.IConnection rabbitConnection = connection.Target.Connection;
        Assert.StartsWith(CFIntegrationConnectionName, rabbitConnection.ClientProvidedName);
    }

    [Fact]
    public void TestDestroy()
    {
        IConnection connection1 = _connectionFactory.CreateConnection();
        _connectionFactory.Destroy();
        IConnection connection2 = _connectionFactory.CreateConnection();
        Assert.Same(connection1, connection2);

        _connectionFactory.Dispose();
        Assert.Throws<RabbitApplicationContextClosedException>(() => _connectionFactory.CreateConnection());
    }

    [Fact]
    public void TestChannelMax()
    {
        _connectionFactory.RabbitConnectionFactory.RequestedChannelMax = 1;
        IConnection connection = _connectionFactory.CreateConnection();
        connection.CreateChannel(true);
        Assert.Throws<RabbitResourceNotAvailableException>(() => connection.CreateChannel());
    }

    public void Dispose()
    {
        _connectionFactory.Destroy();
    }
}
