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
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Support;
using Steeltoe.Messaging.Rabbit.Support.Converter;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitTemplateIntegrationTest : IDisposable
    {
        public const string ROUTE = "test.queue.RabbitTemplateIntegrationTests";
        public const string REPLY_QUEUE_NAME = "test.reply.queue.RabbitTemplateIntegrationTests";

        protected RabbitTemplate template;
        protected RabbitAdmin admin;
        protected string testName;
        private Queue replyQueue = new Queue(REPLY_QUEUE_NAME);
        private CachingConnectionFactory connectionFactory;

        public RabbitTemplateIntegrationTest()
        {
            connectionFactory = new CachingConnectionFactory("localhost");
            connectionFactory.IsPublisherReturns = true;
            template = new RabbitTemplate(connectionFactory);
            template.ReplyTimeout = 10000;
            admin = new RabbitAdmin(connectionFactory);
            admin.DeclareQueue(new Queue(ROUTE));
            admin.DeclareQueue(new Queue(REPLY_QUEUE_NAME));

            // template.SetSendConnectionFactorySelectorExpression(new LiteralExpression("foo"));
            // var mockContext = new Mock<IApplicationContext>();
            // var mockCf = new Mock<IConnectionFactory>();
            // mockCf.Setup(f => f.Username).Returns("guest");
            // mockContext.Setup(c => c.GetService<IConnectionFactory>("cf")).Returns(mockCf.Object);
            // template.ApplicationContext = mockContext.Object;
        }

        public void Dispose()
        {
            admin.DeleteQueue(ROUTE);
            admin.DeleteQueue(REPLY_QUEUE_NAME);
            template.Stop().Wait();
            connectionFactory.Destroy();
        }

        [Fact]
        public void TestChannelCloseInTx()
        {
            connectionFactory.IsPublisherReturns = false;
            var channel = connectionFactory.CreateConnection().CreateChannel(true);
            var holder = new RabbitResourceHolder(channel, true);
            TransactionSynchronizationManager.BindResource(connectionFactory, holder);
            try
            {
                template.IsChannelTransacted = true;
                template.ConvertAndSend(ROUTE, "foo");
                template.ConvertAndSend(Guid.NewGuid().ToString(), ROUTE, "xxx");
                var n = 0;
                while (n++ < 100 && channel.IsOpen)
                {
                    Thread.Sleep(100);
                }

                Assert.False(channel.IsOpen);

                try
                {
                    template.ConvertAndSend(ROUTE, "bar");
                    throw new Exception("Expected exception");
                }
                catch (RabbitUncategorizedException e)
                {
                    if (e.InnerException is InvalidOperationException)
                    {
                        Assert.Contains("Channel closed during transaction", e.InnerException.Message);
                    }
                    else
                    {
                        throw new Exception("Expected InvalidOperationException");
                    }
                }
                catch (RabbitConnectException e)
                {
                    Assert.IsType<R.Exceptions.AlreadyClosedException>(e.InnerException);
                }
            }
            finally
            {
                TransactionSynchronizationManager.UnbindResource(connectionFactory);
                channel.Close();
            }
        }

        [Fact]
        public void TestTemplateUsesPublisherConnectionUnlessInTx()
        {
            connectionFactory.Destroy();
            template.UsePublisherConnection = true;
            template.ConvertAndSend("dummy", "foo");
            Assert.Null(connectionFactory._connection.Target);
            Assert.NotNull(((CachingConnectionFactory)connectionFactory.PublisherConnectionFactory)._connection.Target);
            connectionFactory.Destroy();
            Assert.Null(connectionFactory._connection.Target);
            Assert.Null(((CachingConnectionFactory)connectionFactory.PublisherConnectionFactory)._connection.Target);
            var channel = connectionFactory.CreateConnection().CreateChannel(true);
            Assert.NotNull(connectionFactory._connection.Target);
            var holder = new RabbitResourceHolder(channel, true);
            TransactionSynchronizationManager.BindResource(connectionFactory, holder);
            try
            {
                template.IsChannelTransacted = true;
                template.ConvertAndSend("dummy", "foo");
                Assert.NotNull(connectionFactory._connection.Target);
                Assert.Null(((CachingConnectionFactory)connectionFactory.PublisherConnectionFactory)._connection.Target);
            }
            finally
            {
                TransactionSynchronizationManager.UnbindResource(connectionFactory);
                channel.Close();
            }
        }

        [Fact]
        public void TestReceiveNonBlocking()
        {
            template.ConvertAndSend(ROUTE, "nonblock");
            var n = 0;
            var o = template.ReceiveAndConvert<string>(ROUTE);
            while (n++ < 100 && o == null)
            {
                Thread.Sleep(100);
                o = template.ReceiveAndConvert<string>(ROUTE);
            }

            Assert.NotNull(o);
            Assert.Equal("nonblock", o);
            Assert.Null(template.Receive(ROUTE));
        }

        [Fact]
        public void TestReceiveConsumerCanceled()
        {
            var connectionFactory = new MockSingleConnectionFactory("localhost");
            template = new RabbitTemplate(connectionFactory);
            template.ReceiveTimeout = 10000;
            Assert.Throws<ConsumerCancelledException>(() => template.Receive(ROUTE));
        }

        [Fact]
        public void TestReceiveBlocking()
        {
            // TODO: this.template.setUserIdExpressionString("@cf.username");
            template.ConvertAndSend(ROUTE, "block");
            var received = template.Receive(ROUTE, 10000);
            Assert.NotNull(received);
            Assert.Equal("block", EncodingUtils.Utf8.GetString((byte[])received.Payload));

            // TODO: assertThat(received.getMessageProperties().getReceivedUserId()).isEqualTo("guest");
            template.ReceiveTimeout = 0;
            Assert.Null(template.Receive(ROUTE));
        }

        [Fact]
        public void TestReceiveBlockingNoTimeout()
        {
            template.ConvertAndSend(ROUTE, "blockNoTO");
            var o = template.ReceiveAndConvert<string>(ROUTE, -1);
            Assert.NotNull(o);
            Assert.Equal("blockNoTO", o);
            template.ReceiveTimeout = 1; // test the no message after timeout path
            try
            {
                Assert.Null(template.Receive(ROUTE));
            }
            catch (ConsumeOkNotReceivedException)
            {
                // we're expecting no result, this could happen, depending on timing.
            }
        }

        [Fact]
        public void TestReceiveTimeoutRequeue()
        {
            try
            {
                Assert.Null(template.ReceiveAndConvert<string>(ROUTE, 10));
            }
            catch (ConsumeOkNotReceivedException)
            {
                // empty - race for consumeOk
            }

            Assert.Empty(connectionFactory._cachedChannelsNonTransactional);
        }

        [Fact]
        public void TestReceiveBlockingTx()
        {
            template.ConvertAndSend(ROUTE, "blockTX");
            template.IsChannelTransacted = true;
            template.ReceiveTimeout = 10000;
            var o = template.ReceiveAndConvert<string>(ROUTE);
            Assert.NotNull(o);
            Assert.Equal("blockTX", o);
            template.ReceiveTimeout = 0;
            Assert.Null(template.Receive(ROUTE));
        }

        [Fact]
        public void TestReceiveBlockingGlobalTx()
        {
            template.ConvertAndSend(ROUTE, "blockGTXNoTO");
            RabbitResourceHolder resourceHolder = ConnectionFactoryUtils
                    .GetTransactionalResourceHolder(template.ConnectionFactory, true);
            TransactionSynchronizationManager.SetActualTransactionActive(true);
            ConnectionFactoryUtils.BindResourceToTransaction(resourceHolder, template.ConnectionFactory, true);
            template.ReceiveTimeout = -1;
            template.IsChannelTransacted = true;
            var o = template.ReceiveAndConvert<string>(ROUTE);
            resourceHolder.CommitAll();
            resourceHolder.CloseAll();
            Assert.Same(resourceHolder, TransactionSynchronizationManager.UnbindResource(template.ConnectionFactory));
            Assert.NotNull(o);
            Assert.Equal("blockGTXNoTO", o);
            template.ReceiveTimeout = 0;
            Assert.Null(template.Receive(ROUTE));
        }

        [Fact]
        public void TestSendToNonExistentAndThenReceive()
        {
            // If transacted then the commit fails on send, so we get a nice synchronous exception
            template.IsChannelTransacted = true;
            try
            {
                template.ConvertAndSend(string.Empty, "no.such.route", "message");

                // throw new Exception("Expected RabbitException");
            }
            catch (RabbitException)
            {
                // e.printStackTrace();
            }

            // Now send the real message, and all should be well...
            template.ConvertAndSend(ROUTE, "message");
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendAndReceiveWithPostProcessor()
        {
            template.ConvertAndSend(ROUTE, (object)"message", new PostProccessor1());
            template.SetAfterReceivePostProcessors(new PostProcessor2());
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendAndReceive()
        {
            template.ConvertAndSend(ROUTE, "message");
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendAndReceiveUndeliverable()
        {
            template.Mandatory = true;
            var ex = Assert.Throws<RabbitMessageReturnedException>(() => template.ConvertSendAndReceive<string>(ROUTE + "xxxxxx", "undeliverable"));
            var body = ex.ReturnedMessage.Payload as string;
            Assert.NotNull(body);
            Assert.Equal("undeliverable", body);
            Assert.Contains(ex.ReplyText, "NO_ROUTE");
            Assert.Empty(template._replyHolder);
        }

        [Fact]
        public void TestSendAndReceiveTransacted()
        {
            template.IsChannelTransacted = true;
            template.ConvertAndSend(ROUTE, "message");
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendAndReceiveTransactedWithUncachedConnection()
        {
            var singleConnectionFactory = new SingleConnectionFactory("localhost");
            RabbitTemplate template = new RabbitTemplate(singleConnectionFactory);
            template.IsChannelTransacted = true;
            template.ConvertAndSend(ROUTE, "message");
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
            singleConnectionFactory.Destroy();
        }

        [Fact]
        public void TestSendAndReceiveTransactedWithImplicitRollback()
        {
            template.IsChannelTransacted = true;
            template.ConvertAndSend(ROUTE, "message");

            // Rollback of manual receive is implicit because the channel is
            // closed...
            Assert.Throws<PlannedException>(() => template.Execute((c) =>
            {
                c.BasicGet(ROUTE, false);
                c.BasicRecover(true);
                throw new PlannedException();
            }));
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendAndReceiveInCallback()
        {
            template.ConvertAndSend(ROUTE, "message");
            var messagePropertiesConverter = new DefaultMessageHeadersConverter();
            var result = template.Execute<string>((c) =>
            {
                var response = c.BasicGet(ROUTE, false);
                var props = messagePropertiesConverter.ToMessageHeaders(
                    response.BasicProperties,
                    new Envelope(response.DeliveryTag, response.Redelivered, response.Exchange, response.RoutingKey),
                    EncodingUtils.Utf8);
                c.BasicAck(response.DeliveryTag, false);
                return new SimpleMessageConverter().FromMessage<string>(Message.Create(response.Body, props));
            });

            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestReceiveInExternalTransaction()
        {
            template.ConvertAndSend(ROUTE, "message");
            template.IsChannelTransacted = true;
            var result = new TransactionTemplate(new TestTransactionManager())
                    .Execute(status => template.ReceiveAndConvert<string>(ROUTE));
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestReceiveInExternalTransactionAutoAck()
        {
            template.ConvertAndSend(ROUTE, "message");

            // Should just result in auto-ack (not synched with external tx)
            template.IsChannelTransacted = true;
            var result = new TransactionTemplate(new TestTransactionManager())
                    .Execute(status => template.ReceiveAndConvert<string>(ROUTE));
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestReceiveInExternalTransactionWithRollback()
        {
            // Makes receive (and send in principle) transactional
            template.IsChannelTransacted = true;
            template.ConvertAndSend(ROUTE, "message");
            Assert.Throws<PlannedException>(() =>
            {
                new TransactionTemplate(new TestTransactionManager()).Execute(
                    status =>
                    {
                        template.ReceiveAndConvert<string>(ROUTE);
                        throw new PlannedException();
                    });
            });

            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestReceiveInExternalTransactionWithNoRollback()
        {
            // Makes receive non-transactional
            template.IsChannelTransacted = false;
            template.ConvertAndSend(ROUTE, "message");
            Assert.Throws<PlannedException>(() =>
            {
                new TransactionTemplate(new TestTransactionManager()).Execute(
                    status =>
                    {
                        template.ReceiveAndConvert<string>(ROUTE);
                        throw new PlannedException();
                    });
            });

            // No rollback
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendInExternalTransaction()
        {
            template.IsChannelTransacted = true;
            new TransactionTemplate(new TestTransactionManager()).Execute(status =>
            {
                template.ConvertAndSend(ROUTE, "message");
            });
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestSendInExternalTransactionWithRollback()
        {
            // Makes receive non-transactional
            template.IsChannelTransacted = true;
            Assert.Throws<PlannedException>(() =>
            {
                new TransactionTemplate(new TestTransactionManager()).Execute(
                    status =>
                    {
                        template.ConvertAndSend(ROUTE, "message");
                        throw new PlannedException();
                    });
            });

            // No rollback
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
        }

        [Fact]
        public void TestAtomicSendAndReceive()
        {
            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
            template.DefaultSendDestination = new RabbitDestination(string.Empty, ROUTE);
            template.DefaultSendDestination = new RabbitDestination(ROUTE);
            var task = Task.Run<IMessage>(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = template.Receive();
                    if (message != null)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                Assert.NotNull(message);
                template.Send(message.Headers.ReplyTo(), message);
                return message;
            });
            var message = Message.Create(EncodingUtils.Utf8.GetBytes("test-message"), new MessageHeaders());
            var reply = template.SendAndReceive(message);
            task.Wait(TimeSpan.FromSeconds(10));
            var received = task.Result;
            Assert.NotNull(received);
        }

        protected RabbitTemplate CreateSendAndReceiveRabbitTemplate(IConnectionFactory connectionFactory)
        {
            var template = new RabbitTemplate(connectionFactory);
            template.UseDirectReplyToContainer = false;
            return template;
        }

        private class TestTransactionManager : AbstractPlatformTransactionManager
        {
            protected override void DoBegin(object transaction, ITransactionDefinition definition)
            {
            }

            protected override void DoCommit(DefaultTransactionStatus status)
            {
            }

            protected override object DoGetTransaction()
            {
                return new object();
            }

            protected override void DoRollback(DefaultTransactionStatus status)
            {
            }
        }

        private class PlannedException : Exception
        {
            public PlannedException()
                : base("Planned")
            {
            }
        }

        private class Foo
        {
            public Foo()
            {
            }

            public override string ToString()
            {
                return "FooAsAString";
            }
        }

        private class PostProcessor2 : IMessagePostProcessor
        {
            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                return PostProcessMessage(message);
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                var strings = message.Headers.Get<string[]>("strings");
                Assert.Contains("1", strings);
                Assert.Contains("2", strings);
                var objects = message.Headers.Get<object[]>("objects");
                Assert.Equal("FooAsAString", objects[0]);
                Assert.Equal("FooAsAString", objects[1]);
                var bytes = message.Headers.Get<byte[]>("bytes");
                Assert.Equal("abc", EncodingUtils.Utf8.GetString(bytes));
                return message;
            }
        }

        private class PostProccessor1 : IMessagePostProcessor
        {
            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                return PostProcessMessage(message);
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.ContentType = "text/other";
                accessor.SetHeader("strings", new string[] { "1", "2" });
                accessor.SetHeader("objects", new object[] { new Foo(), new Foo() });
                accessor.SetHeader("bytes", EncodingUtils.Utf8.GetBytes("abc"));
                return message;
            }
        }

        private class MockSingleConnectionFactory : SingleConnectionFactory
        {
            public MockSingleConnectionFactory(string hostname)
                : base(hostname)
            {
            }

            public override Connection.IConnection CreateConnection()
            {
                var dele = base.CreateConnection();
                return new MockConnection(dele);
            }
        }

        private class MockConnection : IConnection
        {
            public MockConnection(Connection.IConnection deleg)
            {
                Delegate = deleg;
            }

            public Connection.IConnection Delegate { get; }

            public bool IsOpen => Delegate.IsOpen;

            public int LocalPort => Delegate.LocalPort;

            public R.IConnection Connection => Delegate.Connection;

            public void AddBlockedListener(IBlockedListener listener)
            {
                Delegate.AddBlockedListener(listener);
            }

            public void Close()
            {
                Delegate.Close();
            }

            public R.IModel CreateChannel(bool transactional = false)
            {
                var chan = Delegate.CreateChannel(transactional);
                return new MockChannel(chan);
            }

            public void Dispose()
            {
                Delegate.Dispose();
            }

            public bool RemoveBlockedListener(IBlockedListener listener)
            {
                return Delegate.RemoveBlockedListener(listener);
            }
        }

        private class MockConsumer : R.IBasicConsumer
        {
            public MockConsumer(R.IBasicConsumer deleg)
            {
                Delegate = deleg;
            }

            public R.IBasicConsumer Delegate { get; }

            public R.IModel Model => Delegate.Model;

            public event EventHandler<R.Events.ConsumerEventArgs> ConsumerCancelled
            {
                add
                {
                    Delegate.ConsumerCancelled += value;
                }

                remove
                {
                    Delegate.ConsumerCancelled -= value;
                }
            }

            public void HandleBasicCancel(string consumerTag)
            {
                Delegate.HandleBasicCancel(consumerTag);
            }

            public void HandleBasicCancelOk(string consumerTag)
            {
                Delegate.HandleBasicCancelOk(consumerTag);
            }

            public void HandleBasicConsumeOk(string consumerTag)
            {
                Delegate.HandleBasicConsumeOk(consumerTag);
                try
                {
                    HandleBasicCancel(consumerTag);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("HandleBasicCancel error", e);
                }
            }

            public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, R.IBasicProperties properties, byte[] body)
            {
                Delegate.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
            }

            public void HandleModelShutdown(object model, R.ShutdownEventArgs reason)
            {
                throw new NotImplementedException();
            }
        }

        private class MockChannel : PublisherCallbackChannel
        {
            public MockChannel(R.IModel channel, ILogger logger = null)
                : base(channel, logger)
            {
            }

            public override string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, R.IBasicConsumer consumer)
            {
                return base.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, new MockConsumer(consumer));
            }
        }
    }
}
