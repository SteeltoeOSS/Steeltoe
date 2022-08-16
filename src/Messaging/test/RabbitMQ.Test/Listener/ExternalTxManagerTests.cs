// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Transaction;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class ExternalTxManagerTests
{
    /// <summary>
    /// Verify that up-stack RabbitTemplate uses listener's channel (MessageListener).
    /// </summary>
    // TODO: Test is incomplete
    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void MessageListenerTest()
#pragma warning restore S2699 // Tests should include assertions
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var onlyChannel = new Mock<RC.IModel>();

        onlyChannel.Setup(m => m.IsOpen).Returns(true);

        var cachingConnectionFactory = new CachingConnectionFactory(mockConnectionFactory.Object);
        mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(m => m.IsOpen).Returns(true);

        mockConnection.Setup(m => m.CreateModel()).Returns(onlyChannel.Object);

        RC.IBasicConsumer consumer;
        var consumerLatch = new CountdownEvent(1);

        onlyChannel
            .Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), null,
                It.IsAny<RC.IBasicConsumer>())).Returns(
                (string _, bool _, string _, bool _, bool _, IDictionary<string, object> _, RC.IBasicConsumer iConsumer) =>
                {
                    consumer = iConsumer;
                    consumerLatch.Signal();
                    return "consumerTag";
                });

        var commitLatch = new CountdownEvent(1);

        onlyChannel.Setup(m => m.TxCommit()).Callback(() =>
        {
            commitLatch.Signal();
        });

        var rollbackEvent = new CountdownEvent(1);

        onlyChannel.Setup(m => m.TxRollback()).Callback(() =>
        {
            rollbackEvent.Signal();
        });

        onlyChannel.Setup(m => m.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));

        var latch = new CountdownEvent(1);
        var container = new DirectMessageListenerContainer(null, cachingConnectionFactory);
        var adapter = new MessageListenerAdapter(null, new TestListener(cachingConnectionFactory, latch));
        container.SetupMessageListener(adapter);

        container.SetQueueNames("queue");
        container.ShutdownTimeout = 100;
        container.TransactionManager = new DummyTxManager();
        container.TransactionAttribute = new DefaultTransactionAttribute();
        container.Initialize();
        container.StartAsync();

        // Assert.True(consumerLatch.Wait(TimeSpan.FromSeconds(10)));
    }

    private sealed class TestListener : IMessageListener
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly CountdownEvent _latch;

        public AcknowledgeMode ContainerAckMode { get; set; }

        public TestListener(IConnectionFactory connectionFactory, CountdownEvent latch)
        {
            _connectionFactory = connectionFactory;
            _latch = latch;
        }

        public void OnMessage(IMessage message)
        {
            var template = new RabbitTemplate(_connectionFactory);
            template.IsChannelTransacted = true;
            template.ConvertAndSend("foo", "bar", "baz");
            _latch.Signal();
        }

        public void OnMessageBatch(IEnumerable<IMessage> messages)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class DummyTxManager : AbstractPlatformTransactionManager
    {
        private readonly CountdownEvent _latch = new(1);

        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
        }

        protected override void DoCommit(DefaultTransactionStatus status)
        {
            _latch.Signal();
        }

        protected override object DoGetTransaction()
        {
            return new object();
        }

        protected override void DoRollback(DefaultTransactionStatus status)
        {
            _latch.Signal();
        }
    }
}
