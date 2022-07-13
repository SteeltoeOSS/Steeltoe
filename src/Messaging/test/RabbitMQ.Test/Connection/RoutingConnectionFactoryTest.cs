// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Listener;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class RoutingConnectionFactoryTest
{
    [Fact]
    public void TestAbstractRoutingConnectionFactory()
    {
        var connectionFactory1 = new Mock<IConnectionFactory>();
        var connectionFactory2 = new Mock<IConnectionFactory>();
        var factories = new Dictionary<object, IConnectionFactory>
        {
            { true, connectionFactory1.Object },
            { false, connectionFactory2.Object }
        };
        var defaultConnectionFactory = new Mock<IConnectionFactory>();

        var lookupFlag = new AtomicBoolean(true);
        var count = new AtomicInteger();
        var connectionFactory = new TestAbstractRoutingConnectionFactoryFactory(lookupFlag, count)
        {
            DefaultTargetConnectionFactory = defaultConnectionFactory.Object
        };
        connectionFactory.SetTargetConnectionFactories(factories);

        for (var i = 0; i < 5; i++)
        {
            connectionFactory.CreateConnection();
        }

        connectionFactory1.Verify(f => f.CreateConnection(), Times.Exactly(2));
        connectionFactory2.Verify(f => f.CreateConnection());
        defaultConnectionFactory.Verify(f => f.CreateConnection(), Times.Exactly(2));
    }

    [Fact]
    public void TestSimpleRoutingConnectionFactory()
    {
        var connectionFactory1 = new Mock<IConnectionFactory>();
        var connectionFactory2 = new Mock<IConnectionFactory>();
        var factories = new Dictionary<object, IConnectionFactory>
        {
            { "foo", connectionFactory1.Object },
            { "bar", connectionFactory2.Object }
        };

        var connectionFactory = new SimpleRoutingConnectionFactory();
        connectionFactory.SetTargetConnectionFactories(factories);
        var tasks = new List<Task>();
        for (var i = 0; i < 3; i++)
        {
            var count = i;
            var task = Task.Run(() =>
            {
                SimpleResourceHolder.Bind(connectionFactory, count % 2 == 0 ? "foo" : "bar");
                connectionFactory.CreateConnection();
                SimpleResourceHolder.Unbind(connectionFactory);
            });
            tasks.Add(task);
        }

        Assert.True(Task.WaitAll(tasks.ToArray(), 10000));
        connectionFactory1.Verify(f => f.CreateConnection(), Times.Exactly(2));
        connectionFactory2.Verify(f => f.CreateConnection());
    }

    [Fact]
    public void TestGetAddAndRemoveOperationsForTargetConnectionFactories()
    {
        var targetConnectionFactory = new Mock<IConnectionFactory>();
        var routingFactory = new TestNullReturningFactory();
        Assert.Null(routingFactory.GetTargetConnectionFactory("1"));
        routingFactory.AddTargetConnectionFactory("1", targetConnectionFactory.Object);
        Assert.Equal(targetConnectionFactory.Object, routingFactory.GetTargetConnectionFactory("1"));
        Assert.Null(routingFactory.GetTargetConnectionFactory("2"));
        var removedConnectionFactory = routingFactory.RemoveTargetConnectionFactory("1");
        Assert.Equal(targetConnectionFactory.Object, removedConnectionFactory);
        Assert.Null(routingFactory.GetTargetConnectionFactory("1"));
    }

    [Fact]
    public void TestAddTargetConnectionFactoryAddsExistingConnectionListenersToConnectionFactory()
    {
        var routingFactory = new TestNullReturningFactory();
        routingFactory.AddConnectionListener(new Mock<IConnectionListener>().Object);
        routingFactory.AddConnectionListener(new Mock<IConnectionListener>().Object);
        var targetConnectionFactory = new Mock<IConnectionFactory>();
        routingFactory.AddTargetConnectionFactory("1", targetConnectionFactory.Object);
        targetConnectionFactory.Verify(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task TestAbstractRoutingConnectionFactoryWithListenerContainer()
    {
        var connectionFactory1 = new Mock<IConnectionFactory>();
        var connectionFactory2 = new Mock<IConnectionFactory>();
        var defaultConnectionFactory = new Mock<IConnectionFactory>();
        var connection1 = new Mock<IConnection>();
        var connection2 = new Mock<IConnection>();
        var defaultConnection = new Mock<IConnection>();
        var channel1 = new Mock<RC.IModel>();
        var channel2 = new Mock<RC.IModel>();
        var defaultChannel = new Mock<RC.IModel>();

        connectionFactory1.SetupSequence(f => f.CreateConnection())
            .Returns(connection1.Object);

        connectionFactory2.SetupSequence(f => f.CreateConnection())
            .Returns(connection1.Object)
            .Returns(connection2.Object);

        defaultConnectionFactory.SetupSequence(f => f.CreateConnection())
            .Returns(defaultConnection.Object);

        connection1.Setup(c => c.IsOpen).Returns(true);
        connection1.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel1.Object);

        connection2.Setup(c => c.IsOpen).Returns(true);
        connection2.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel2.Object);

        defaultConnection.Setup(c => c.IsOpen).Returns(true);
        defaultConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(defaultChannel.Object);

        channel1.Setup(c => c.IsOpen).Returns(true);
        channel2.Setup(c => c.IsOpen).Returns(true);
        defaultChannel.Setup(c => c.IsOpen).Returns(true);

        var factories = new Dictionary<object, IConnectionFactory>
        {
            { "[baz]", connectionFactory1.Object },
            { "[foo,bar]", connectionFactory2.Object }
        };

        var connectionFactory = new SimpleRoutingConnectionFactory
        {
            LenientFallback = true,
            DefaultTargetConnectionFactory = defaultConnectionFactory.Object
        };
        connectionFactory.SetTargetConnectionFactories(factories);

        var container = new DirectMessageListenerContainer(null, connectionFactory);
        container.SetQueueNames("foo", "bar");
        container.Initialize();
        await container.Start();

        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10)));

        connectionFactory1.Verify(f => f.CreateConnection(), Times.Never);
        connectionFactory2.Verify(f => f.CreateConnection(), Times.Exactly(2));
        defaultConnectionFactory.Verify(f => f.CreateConnection(), Times.Once); // Checks connection

        connectionFactory1.Invocations.Clear();
        connectionFactory2.Invocations.Clear();
        defaultConnectionFactory.Invocations.Clear();

        container.SetQueueNames("baz");
        connectionFactory1.Verify(f => f.CreateConnection());
        connectionFactory2.Verify(f => f.CreateConnection(), Times.Never);
        defaultConnectionFactory.Verify(f => f.CreateConnection(), Times.Never);

        connectionFactory1.Invocations.Clear();
        connectionFactory2.Invocations.Clear();
        defaultConnectionFactory.Invocations.Clear();

        container.SetQueueNames("qux");
        connectionFactory1.Verify(f => f.CreateConnection(), Times.Never);
        connectionFactory2.Verify(f => f.CreateConnection(), Times.Never);
        defaultConnectionFactory.Verify(f => f.CreateConnection());

        await container.Stop();
    }

    [Fact]
    public async Task TestWithDmlcAndConnectionListener()
    {
        var connectionFactory1 = new Mock<IConnectionFactory>();
        var connection1 = new Mock<IConnection>();
        var channel1 = new Mock<RC.IModel>();

        var factories = new Dictionary<object, IConnectionFactory>
        {
            { "xxx[foo]", connectionFactory1.Object }
        };

        var connectionFactory = new SimpleRoutingConnectionFactory();

        connection1.Setup(c => c.IsOpen).Returns(true);
        connection1.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel1.Object);

        channel1.Setup(c => c.IsOpen).Returns(true);

        var connectionMakerKey1 = new AtomicReference<object>();
        var latch = new CountdownEvent(1);
        connectionFactory1.Setup(f => f.CreateConnection())
            .Returns(connection1.Object)
            .Callback(() =>
            {
                connectionMakerKey1.Value = connectionFactory.DetermineCurrentLookupKey();
                latch.Signal();
            });
        connectionFactory.SetTargetConnectionFactories(factories);
        var connectionMakerKey2 = new AtomicReference<object>();

        var container = new TestDirectMessageListenerContainer(connectionFactory, connectionMakerKey2);
        container.SetQueueNames("foo");
        container.LookupKeyQualifier = "xxx";
        container.ShutdownTimeout = 10;
        container.Initialize();
        await container.Start();

        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10))); // Container started
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));

        await container.Stop();
        Assert.Equal("xxx[foo]", connectionMakerKey1.Value);
        Assert.Equal("xxx[foo]", connectionMakerKey2.Value);
    }

    [Fact]
    public async Task TestWithDrtDmlcAndConnectionListenerExistingRfk()
    {
        var connectionFactory1 = new Mock<IConnectionFactory>();
        var connection1 = new Mock<IConnection>();
        var channel1 = new Mock<RC.IModel>();

        var factories = new Dictionary<object, IConnectionFactory>
        {
            { "xxx[foo]", connectionFactory1.Object },
            { "xxx[amq.rabbitmq.reply-to]", connectionFactory1.Object }
        };

        var connectionFactory = new SimpleRoutingConnectionFactory();
        SimpleResourceHolder.Bind(connectionFactory, "foo");

        connection1.Setup(c => c.IsOpen).Returns(true);
        connection1.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel1.Object);

        channel1.Setup(c => c.IsOpen).Returns(true);

        var connectionMakerKey1 = new AtomicReference<object>();
        var latch = new CountdownEvent(1);
        connectionFactory1.Setup(f => f.CreateConnection())
            .Returns(connection1.Object)
            .Callback(() =>
            {
                connectionMakerKey1.Value = connectionFactory.DetermineCurrentLookupKey();
                latch.Signal();
            });
        connectionFactory.SetTargetConnectionFactories(factories);
        var connectionMakerKey2 = new AtomicReference<object>();

        var container = new TestDirectReplyToMessageListenerContainer(connectionFactory, connectionMakerKey2)
        {
            LookupKeyQualifier = "xxx",
            ShutdownTimeout = 10
        };
        container.Initialize();
        await container.Start();

        Assert.True(container.StartedLatch.Wait(TimeSpan.FromSeconds(10))); // Container started

        var channelHolder = container.GetChannelHolder();
        Assert.True(latch.Wait(TimeSpan.FromSeconds(10)));
        container.ReleaseConsumerFor(channelHolder, true, "test");
        await container.Stop();

        Assert.Equal("xxx[amq.rabbitmq.reply-to]", connectionMakerKey1.Value);
        Assert.Equal("xxx[amq.rabbitmq.reply-to]", connectionMakerKey2.Value);
        Assert.Equal("foo", SimpleResourceHolder.Unbind(connectionFactory));
    }

    private sealed class TestNullReturningFactory : AbstractRoutingConnectionFactory
    {
        public override string ServiceName { get; set; } = "TestNullReturningFactory";

        public override object DetermineCurrentLookupKey()
        {
            return null;
        }
    }

    private sealed class TestDirectReplyToMessageListenerContainer : DirectReplyToMessageListenerContainer
    {
        private readonly AtomicReference<object> _connectionMakerKey;
        private readonly SimpleRoutingConnectionFactory _simpleFactory;

        public TestDirectReplyToMessageListenerContainer(SimpleRoutingConnectionFactory connectionFactory, AtomicReference<object> connectionMakerKey)
            : base(null, connectionFactory)
        {
            _connectionMakerKey = connectionMakerKey;
            _simpleFactory = connectionFactory;
        }

        protected override void RedeclareElementsIfNecessary()
        {
            _connectionMakerKey.Value = _simpleFactory.DetermineCurrentLookupKey();
        }
    }

    private sealed class TestDirectMessageListenerContainer : DirectMessageListenerContainer
    {
        private readonly AtomicReference<object> _connectionMakerKey2;
        private readonly SimpleRoutingConnectionFactory _simpleFactory;

        public TestDirectMessageListenerContainer(SimpleRoutingConnectionFactory connectionFactory, AtomicReference<object> connectionMakerKey2)
            : base(null, connectionFactory)
        {
            _connectionMakerKey2 = connectionMakerKey2;
            _simpleFactory = connectionFactory;
        }

        protected override void RedeclareElementsIfNecessary()
        {
            _connectionMakerKey2.Value = _simpleFactory.DetermineCurrentLookupKey();
        }
    }

    private sealed class TestAbstractRoutingConnectionFactoryFactory : AbstractRoutingConnectionFactory
    {
        private readonly AtomicBoolean _lookupFlag;
        private readonly AtomicInteger _count;

        public TestAbstractRoutingConnectionFactoryFactory(AtomicBoolean lookupFlag, AtomicInteger count)
        {
            _lookupFlag = lookupFlag;
            _count = count;
        }

        public override string ServiceName { get; set; } = "TestAbstractRoutingConnectionFactoryFactory";

        public override object DetermineCurrentLookupKey()
        {
            return _count.IncrementAndGet() > 3 ? null : (bool?)_lookupFlag.GetAndSet(!_lookupFlag.Value);
        }
    }
}
