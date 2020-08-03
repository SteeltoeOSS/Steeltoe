using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using Xunit;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class ExternalTxManagerTests
    {
        /// <summary>
        /// Verify that up-stack RabbitTemplate uses listener's channel (MessageListener)
        /// </summary>
        /// TODO: Test is incomplete
        [Fact]
        public void MessageListenerTest()
        {
            var mockConnectionFactory = new Mock<RabbitMQ.Client.IConnectionFactory>();
            var mockConnection = new Mock<RabbitMQ.Client.IConnection>();
            var onlyChannel = new Mock<IModel>();

            onlyChannel.Setup(m => m.IsOpen).Returns(true);

            var tooManyModels = new Exception();

            var cachingConnectionFactory = new CachingConnectionFactory(mockConnectionFactory.Object);
            mockConnectionFactory.Setup(m => m.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(m => m.IsOpen).Returns(true);
            Func<IModel> ensureOneModel = EnsureOneModel(onlyChannel.Object, tooManyModels);

            mockConnection.Setup(m => m.CreateModel()).Returns(onlyChannel.Object);

            IBasicConsumer consumer;
            CountdownEvent consumerLatch = new CountdownEvent(1);
            onlyChannel.Setup(m => m.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), null, It.IsAny<IBasicConsumer>()))
                .Returns((string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer iConsumer) =>
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
            //var template = new RabbitTemplate(cachingConnectionFactory);
            //template.ConvertAndSend("foo", "bar", "baz");

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

        private Func<IModel> EnsureOneModel(IModel onlyModel, Exception tooManyChannels)
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
                var modelMock = new Mock<IModel>();
                modelMock.Setup(m => m.IsOpen).Returns(true);
                return modelMock.Object;
            };
        }

        private class TestListener : IMessageListener
        {
            private Connection.IConnectionFactory _connectionFactory;
            private CountdownEvent _latch;

            public TestListener(Connection.IConnectionFactory connectionFactory, CountdownEvent latch)
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

        private class DummyTxManager : AbstractPlatformTransactionManager
        {
            private volatile bool _committed;
            private volatile bool _rolledBack;
            private volatile CountdownEvent _latch = new CountdownEvent(1);

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
                this._rolledBack = true;
                this._latch.Signal();
            }
        }
    }
}
