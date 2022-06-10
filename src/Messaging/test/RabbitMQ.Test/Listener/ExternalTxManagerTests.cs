// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class ExternalTxManagerTests
{
    /// <summary>
    /// Verify that up-stack RabbitTemplate uses listener's channel (MessageListener)
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

        var tooManyModels = new Exception();

        var cachingConnectionFactory = new CachingConnectionFactory(mockConnectionFactory.Object);
        mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(m => m.IsOpen).Returns(true);
        var ensureOneModel = EnsureOneModel(onlyChannel.Object, tooManyModels);

        mockConnection.Setup(m => m.CreateModel()).Returns(onlyChannel.Object);

        RC.IBasicConsumer consumer;
        var consumerLatch = new CountdownEvent(1);
        onlyChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), null, It.IsAny<RC.IBasicConsumer>()))
            .Returns((string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, RC.IBasicConsumer iConsumer) =>
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
        container.Start();

        // Assert.True(consumerLatch.Wait(TimeSpan.FromSeconds(10)));
    }

    private Func<RC.IModel> EnsureOneModel(RC.IModel onlyModel, Exception tooManyChannels)
    {
        var done = new AtomicBoolean();
        return () =>
        {
            if (!done.Value)
            {
                done.Value = true;
                return onlyModel;
            }

            tooManyChannels = new Exception("More than one Model requested");
            var modelMock = new Mock<RC.IModel>();
            modelMock.Setup(m => m.IsOpen).Returns(true);
            return modelMock.Object;
        };
    }

    private sealed class TestListener : IMessageListener
    {
        private IConnectionFactory _connectionFactory;
        private CountdownEvent _latch;

        public TestListener(IConnectionFactory connectionFactory, CountdownEvent latch)
        {
            _connectionFactory = connectionFactory;
            _latch = latch;
        }

        public AcknowledgeMode ContainerAckMode { get; set; }

        public void OnMessage(IMessage message)
        {
            var template = new RabbitTemplate(_connectionFactory);
            template.IsChannelTransacted = true;
            template.ConvertAndSend("foo", "bar", "baz");
            _latch.Signal();
        }

        public void OnMessageBatch(List<IMessage> messages)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class DummyTxManager : AbstractPlatformTransactionManager
    {
        private volatile bool _committed;
        private volatile bool _rolledBack;
        private volatile CountdownEvent _latch = new (1);

        public bool Committed => _committed;

        public bool RolledBack => _rolledBack;

        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
        }

        protected override void DoCommit(DefaultTransactionStatus status)
        {
            _committed = true;
            _latch.Signal();
        }

        protected override object DoGetTransaction()
        {
            return new object();
        }

        protected override void DoRollback(DefaultTransactionStatus status)
        {
            _rolledBack = true;
            _latch.Signal();
        }
    }
}
