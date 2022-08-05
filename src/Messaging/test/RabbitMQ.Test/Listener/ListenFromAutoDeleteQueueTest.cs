// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

[Trait("Category", "Integration")]
public sealed class ListenFromAutoDeleteQueueTest : IDisposable
{
    public const string Exchange1 = "testContainerWithAutoDeleteQueues";
    public const string Exchange2 = "otherExchange";
    public const string Q1 = "anon";
    public const string Q2 = "anon2";
    public const string Q3 = "otherAnon";

    private readonly DirectMessageListenerContainer _listenerContainer1;
    private readonly DirectMessageListenerContainer _listenerContainer2;
    private readonly DirectMessageListenerContainer _listenerContainer3;
    private readonly DirectMessageListenerContainer _listenerContainer4;

    private readonly Queue _expiringQueue;
    private readonly IConnectionFactory _connectionFactory;
    private readonly AppendingListener _listener;
    private readonly TestAdmin _containerAdmin;

    public ListenFromAutoDeleteQueueTest()
    {
        _connectionFactory = new CachingConnectionFactory("localhost")
        {
            IsPublisherReturns = true
        };

        // Container Admin
        _containerAdmin = new TestAdmin(_connectionFactory);

        // Exchange
        var directExchange = new DirectExchange("testContainerWithAutoDeleteQueues", true, true);

        _listenerContainer1 = new DirectMessageListenerContainer(null, _connectionFactory, "container1");
        _listenerContainer1.ConsumersPerQueue = 2;
        _listenerContainer1.AddQueueNames(Q1, Q2);
        _containerAdmin.DeclareExchange(directExchange);
        _containerAdmin.DeclareQueue(new Queue(Q1, true, false, true));
        _containerAdmin.DeclareQueue(new Queue(Q2, true, false, true));
        _containerAdmin.DeclareBinding(new Binding("b1", Q1, Binding.DestinationType.Queue, directExchange.ExchangeName, Q1, null));
        _containerAdmin.DeclareBinding(new Binding("b2", Q2, Binding.DestinationType.Queue, directExchange.ExchangeName, Q2, null));

        // Listener
        _listener = new AppendingListener();
        var adapter = new MessageListenerAdapter(null, _listener);
        _listenerContainer1.MessageListener = adapter;
        _listenerContainer1.StartAsync();
        _listenerContainer1.StartedLatch.Wait(TimeSpan.FromSeconds(10));

        // Conditional declarations
        var otherExchange = new DirectExchange(Exchange2, true, true);
        _containerAdmin.DeclareExchange(otherExchange);
        _containerAdmin.DeclareQueue(new Queue(Q3, true, false, true));
        _containerAdmin.DeclareBinding(new Binding("b3", Q3, Binding.DestinationType.Queue, otherExchange.ExchangeName, Q3, null));

        _listenerContainer2 = new DirectMessageListenerContainer(null, _connectionFactory, "container2");
        _listenerContainer2.IsAutoStartup = false;
        _listenerContainer2.ShutdownTimeout = 50;
        _listenerContainer2.AddQueueNames(Q3);
        _listenerContainer2.MessageListener = adapter;

        _expiringQueue = new Queue(Guid.NewGuid().ToString(), true, false, false, new Dictionary<string, object>
        {
            { "x-expires", 200 }
        });

        _containerAdmin.DeclareQueue(_expiringQueue);
        _listenerContainer3 = new DirectMessageListenerContainer(null, _connectionFactory, "container3");
        _listenerContainer3.IsAutoStartup = false;
        _listenerContainer3.ShutdownTimeout = 50;
        _listenerContainer3.AddQueueNames(_expiringQueue.QueueName);
        _listenerContainer3.MessageListener = adapter;

        _listenerContainer4 = new DirectMessageListenerContainer(null, _connectionFactory, "container4");

        _listenerContainer4.IsAutoStartup = false;
        _listenerContainer4.ShutdownTimeout = 50;
        _listenerContainer4.AddQueueNames(Q2);
        _listenerContainer4.MessageListener = adapter;
        _listenerContainer4.AutoDeclare = false;
    }

    [Fact]
    public void TestStopStart()
    {
        var rabbitTemplate = new RabbitTemplate(_connectionFactory);
        rabbitTemplate.ConvertAndSend(Exchange1, Q1, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);
        _listenerContainer1.StopAsync();
        _listenerContainer1.StartAsync();
        _listenerContainer1.StartedLatch.Wait(TimeSpan.FromSeconds(10));
        rabbitTemplate.ConvertAndSend(Exchange1, Q1, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);
    }

    [Fact]
    public void TestStopStartConditionalDeclarations()
    {
        var rabbitTemplate = new RabbitTemplate(_connectionFactory);
        _listenerContainer2.StartAsync();
        _listenerContainer2.StartedLatch.Wait(TimeSpan.FromSeconds(10));

        rabbitTemplate.ConvertAndSend(Exchange2, Q3, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);
        _listenerContainer2.StopAsync();
        _listenerContainer2.StartAsync();
        _listenerContainer1.StartedLatch.Wait(TimeSpan.FromSeconds(10));
        rabbitTemplate.ConvertAndSend(Exchange2, Q3, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);
    }

    [Fact]
    public void TestRedeclareXExpiresQueue()
    {
        var rabbitTemplate = new RabbitTemplate(_connectionFactory);
        _listenerContainer3.StartAsync();
        _listenerContainer3.StartedLatch.Wait(TimeSpan.FromSeconds(10));
        rabbitTemplate.ConvertAndSend(_expiringQueue.QueueName, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);

        _listenerContainer3.StopAsync();
        _listenerContainer3.StartAsync();
        _listenerContainer3.StartedLatch.Wait(TimeSpan.FromSeconds(10));

        rabbitTemplate.ConvertAndSend(_expiringQueue.QueueName, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);
    }

    [Fact]
    public void TestAutoDeclareFalse()
    {
        var rabbitTemplate = new RabbitTemplate(_connectionFactory);
        rabbitTemplate.ConvertAndSend(Exchange1, Q2, "foo");
        _listener.Latch.Wait(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(_listener.Queue);

        _listenerContainer4.StopAsync();
        var testAdminMock = new Mock<IRabbitAdmin>();
        testAdminMock.Setup(m => m.Initialize()).Throws(new Exception("Should not be called!"));
        _listenerContainer4.RabbitAdmin = testAdminMock.Object;
        _listenerContainer4.StopAsync();
        _listenerContainer4.StartAsync();
        testAdminMock.Verify(m => m.Initialize(), Times.Never);
    }

    public void Dispose()
    {
        _containerAdmin.DeleteQueue(Q1);
        _containerAdmin.DeleteQueue(Q2);
        _containerAdmin.DeleteQueue(Q3);
        _containerAdmin.DeleteQueue(_expiringQueue.ActualName);
        _containerAdmin.DeleteExchange("testContainerWithAutoDeleteQueues");
        _containerAdmin.DeleteExchange("otherExchange");
        _connectionFactory.Dispose();
        _listenerContainer1.Dispose();
        _listenerContainer2.Dispose();
        _listenerContainer3.Dispose();
        _listenerContainer4.Dispose();
    }

    private sealed class AppendingListener : IReplyingMessageListener<string, string>
    {
        public readonly ConcurrentQueue<string> Queue = new();
        public readonly CountdownEvent Latch = new(1);

        public string HandleMessage(string input)
        {
            Queue.Enqueue(input);
            Latch.Signal();
            return input;
        }
    }

    private sealed class TestAdmin : RabbitAdmin
    {
        public TestAdmin(IConnectionFactory connectionFactory, ILogger logger = null)
            : base(connectionFactory, logger)
        {
        }
    }
}
