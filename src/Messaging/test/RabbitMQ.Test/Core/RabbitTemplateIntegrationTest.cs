// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core
{
    [Trait("Category", "Integration")]
    public class RabbitTemplateIntegrationTest : IDisposable
    {
        public const string ROUTE = "test.queue.RabbitTemplateIntegrationTests";
        public const string REPLY_QUEUE_NAME = "test.reply.queue.RabbitTemplateIntegrationTests";

        protected RabbitTemplate template;
        protected RabbitTemplate routingTemplate;
        protected RabbitAdmin admin;
        protected Mock<IConnectionFactory> cf1;
        protected Mock<IConnectionFactory> cf2;
        protected Mock<IConnectionFactory> defaultCF;

        private readonly CachingConnectionFactory connectionFactory;

        public RabbitTemplateIntegrationTest()
        {
            connectionFactory = new CachingConnectionFactory("localhost")
            {
                IsPublisherReturns = true
            };
            template = new RabbitTemplate(connectionFactory)
            {
                ReplyTimeout = 10000
            };

            // template.SetSendConnectionFactorySelectorExpression(new LiteralExpression("foo"));
            var adminCf = new CachingConnectionFactory("localhost");
            admin = new RabbitAdmin(adminCf);
            admin.DeclareQueue(new Queue(ROUTE));
            admin.DeclareQueue(new Queue(REPLY_QUEUE_NAME));

            routingTemplate = new RabbitTemplate();

            // TODO: Requires expression language support
            // routingTemplate.SendConnectionFactorySelectorExpression = "messageProperties.headers['cfKey']"
            var routingConnFactory = new SimpleRoutingConnectionFactory();
            cf1 = new Mock<IConnectionFactory>();
            cf2 = new Mock<IConnectionFactory>();
            defaultCF = new Mock<IConnectionFactory>();
            routingConnFactory.AddTargetConnectionFactory("foo", cf1.Object);
            routingConnFactory.AddTargetConnectionFactory("bar", cf2.Object);
            routingTemplate.ConnectionFactory = routingConnFactory;
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
                    Assert.IsType<RC.Exceptions.AlreadyClosedException>(e.InnerException);
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
            template = new RabbitTemplate(connectionFactory)
            {
                ReceiveTimeout = 10000
            };
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
            var resourceHolder = ConnectionFactoryUtils
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
            var ex = Assert.Throws<RabbitMessageReturnedException>(() => template.ConvertSendAndReceive<string>($"{ROUTE}xxxxxx", "undeliverable"));
            var body = ex.ReturnedMessage.Payload as byte[];
            Assert.NotNull(body);
            Assert.Equal("undeliverable", EncodingUtils.Utf8.GetString(body));
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
            var template = new RabbitTemplate(singleConnectionFactory)
            {
                IsChannelTransacted = true
            };
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
            var ex = Assert.Throws<RabbitUncategorizedException>(() => template.Execute((c) =>
            {
                c.BasicGet(ROUTE, false);
                c.BasicRecover(true);
                throw new PlannedException();
            }));
            Assert.IsType<PlannedException>(ex.InnerException);
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
            var result = template.Execute((c) =>
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
                        var result = template.ReceiveAndConvert<string>(ROUTE);
                        Assert.NotNull(result);
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
            template.DefaultReceiveDestination = new RabbitDestination(ROUTE);
            var task = Task.Run(() =>
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

        [Fact]
        public void TestAtomicSendAndReceiveUserCorrelation()
        {
            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
            template.DefaultSendDestination = new RabbitDestination(string.Empty, ROUTE);
            template.DefaultReceiveDestination = new RabbitDestination(ROUTE);
            var remoteCorrelationId = new AtomicReference<string>();
            var received = Task.Run(() =>
            {
                var message = template.Receive(10000);
                Assert.NotNull(message);
                remoteCorrelationId.Value = message.Headers.CorrelationId();
                template.Send(message.Headers.ReplyTo(), message);
                return message;
            });
            var admin = new RabbitAdmin(cachingConnectionFactory);
            var replyQueue = admin.DeclareQueue();
            template.ReplyAddress = replyQueue.QueueName;
            template.UserCorrelationId = true;
            template.ReplyTimeout = 10000;
            var container = new DirectMessageListenerContainer(null, cachingConnectionFactory);
            container.SetQueues(replyQueue);
            container.MessageListener = template;
            container.Initialize();
            container.Start();
            var headers = new RabbitHeaderAccessor(new MessageHeaders()) { CorrelationId = "myCorrelationId" };
            var message = Message.Create(Encoding.UTF8.GetBytes("test-message"), headers.MessageHeaders);
            var reply = template.SendAndReceive(message);
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])received.Result.Payload));
            Assert.NotNull(reply);
            Assert.Equal("myCorrelationId", remoteCorrelationId.Value);
            Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])reply.Payload));
            reply = template.Receive();
            Assert.Null(reply);
            template.Stop().Wait();
            container.Stop().Wait();
            cachingConnectionFactory.Destroy();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithRoutingKey()
        {
            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);

            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = template.Receive(ROUTE);
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
            var message = Message.Create(Encoding.UTF8.GetBytes("test-message"), new MessageHeaders());
            var reply = template.SendAndReceive(ROUTE, message);
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])received.Result.Payload));
            Assert.NotNull(reply);
            Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])reply.Payload));
            reply = template.Receive(ROUTE);
            Assert.Null(reply);
            template.Stop().Wait();
            cachingConnectionFactory.Destroy();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithExchangeAndRoutingKey()
        {
            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);

            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = template.Receive(ROUTE);
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
            var message = Message.Create(Encoding.UTF8.GetBytes("test-message"), new MessageHeaders());
            var reply = template.SendAndReceive(string.Empty, ROUTE, message);
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])received.Result.Payload));
            Assert.NotNull(reply);
            Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])reply.Payload));
            reply = template.Receive(ROUTE);
            Assert.Null(reply);
            template.Stop().Wait();
            cachingConnectionFactory.Destroy();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithConversion()
        {
            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
            template.RoutingKey = ROUTE;
            template.DefaultReceiveQueue = ROUTE;

            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = template.Receive(ROUTE);
                    if (message != null)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                Assert.NotNull(message);
                template.Send(message.Headers.ReplyTo(), message);
                return template.MessageConverter.FromMessage<string>(message);
            });
            var result = template.ConvertSendAndReceive<string>("message");
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("message", received.Result);
            result = template.ReceiveAndConvert<string>();
            Assert.Null(result);
            template.Stop().Wait();
            cachingConnectionFactory.Destroy();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithConversionUsingRoutingKey()
        {
            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = this.template.Receive(ROUTE);
                    if (message != null)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                Assert.NotNull(message);
                this.template.Send(message.Headers.ReplyTo(), message);
                return this.template.MessageConverter.FromMessage<string>(message);
            });
            var template = CreateSendAndReceiveRabbitTemplate(connectionFactory);
            var result = template.ConvertSendAndReceive<string>(ROUTE, "message");
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("message", received.Result);
            Assert.Equal("message", result);

            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
            template.Stop().Wait();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithConversionUsingExchangeAndRoutingKey()
        {
            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = this.template.Receive(ROUTE);
                    if (message != null)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                Assert.NotNull(message);
                this.template.Send(message.Headers.ReplyTo(), message);
                return this.template.MessageConverter.FromMessage<string>(message);
            });
            var template = CreateSendAndReceiveRabbitTemplate(connectionFactory);
            var result = template.ConvertSendAndReceive<string>(string.Empty, ROUTE, "message");
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("message", received.Result);
            Assert.Equal("message", result);

            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
            template.Stop().Wait();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessor()
        {
            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
            template.RoutingKey = ROUTE;
            template.DefaultReceiveQueue = ROUTE;

            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
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
                return template.MessageConverter.FromMessage<string>(message);
            });
            var result = template.ConvertSendAndReceive<string>((object)"message", new PostProcessor3());
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("MESSAGE", received.Result);
            Assert.Equal("MESSAGE", result);

            result = template.ReceiveAndConvert<string>();
            Assert.Null(result);
            template.Stop().Wait();
            cachingConnectionFactory.Destroy();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessorUsingRoutingKey()
        {
            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = this.template.Receive(ROUTE);
                    if (message != null)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                Assert.NotNull(message);
                this.template.Send(message.Headers.ReplyTo(), message);
                return this.template.MessageConverter.FromMessage<string>(message);
            });
            var template = CreateSendAndReceiveRabbitTemplate(connectionFactory);
            var result = template.ConvertSendAndReceive<string>(ROUTE, (object)"message", new PostProcessor3());
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("MESSAGE", received.Result);
            Assert.Equal("MESSAGE", result);

            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
            template.Stop().Wait();
        }

        [Fact]
        public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessorUsingExchangeAndRoutingKey()
        {
            // Set up a consumer to respond to our producer
            var received = Task.Run(() =>
            {
                IMessage message = null;
                for (var i = 0; i < 10; i++)
                {
                    message = this.template.Receive(ROUTE);
                    if (message != null)
                    {
                        break;
                    }

                    Thread.Sleep(100);
                }

                Assert.NotNull(message);
                this.template.Send(message.Headers.ReplyTo(), message);
                return this.template.MessageConverter.FromMessage<string>(message);
            });
            var template = CreateSendAndReceiveRabbitTemplate(connectionFactory);
            var result = template.ConvertSendAndReceive<string>(string.Empty, ROUTE, "message", new PostProcessor3());
            Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
            Assert.Equal("MESSAGE", received.Result);
            Assert.Equal("MESSAGE", result);

            result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Null(result);
            template.Stop().Wait();
        }

        [Fact]
        public void TtestReceiveAndReplyNonStandardCorrelationNotBytes()
        {
            template.DefaultReceiveQueue = ROUTE;
            template.RoutingKey = ROUTE;
            var headers = new MessageHeaders(new Dictionary<string, object> { { "baz", "bar" } });
            var message = Message.Create(Encoding.UTF8.GetBytes("foo"), headers);
            template.Send(ROUTE, message);
            template.CorrelationKey = "baz";
            var received = template.ReceiveAndReply<IMessage, IMessage>((message1) => Message.Create(Encoding.UTF8.GetBytes("fuz"), new MessageHeaders()));
            Assert.True(received);
            var message2 = template.Receive();
            Assert.NotNull(message2);
            Assert.Equal("bar", message2.Headers.Get<string>("baz"));
        }

        [Fact]
        public void TestReceiveAndReplyBlocking()
        {
            TestReceiveAndReply(10000);
        }

        [Fact]
        public void TestReceiveAndReplyNonBlocking()
        {
            TestReceiveAndReply(0);
        }

        [Fact]
        public void TestSymmetricalReceiveAndReply()
        {
            var template = CreateSendAndReceiveRabbitTemplate(connectionFactory);
            template.DefaultReceiveQueue = ROUTE;
            template.RoutingKey = ROUTE;
            template.ReplyAddress = REPLY_QUEUE_NAME;
            template.ReplyTimeout = 20000;
            template.ReceiveTimeout = 20000;

            var container = new DirectMessageListenerContainer();
            container.ConnectionFactory = template.ConnectionFactory;
            container.SetQueueNames(REPLY_QUEUE_NAME);
            container.MessageListener = template;
            container.Start().Wait();

            var count = 10;
            var results = new ConcurrentDictionary<double, object>();
            template.CorrelationKey = "CorrelationKey";
            var tasks = new List<Task>();
            for (var i = 0; i < count; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random();
                    var request = random.NextDouble() * 100;
                    var reply = template.ConvertSendAndReceive<object>(request);
                    results.TryAdd(request, reply);
                }));
            }

            for (var i = 0; i < count; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var random = new Random();
                    var request = random.NextDouble() * 100;
                    var messageHeaders = new RabbitHeaderAccessor(new MessageHeaders()) { ContentType = MessageHeaders.CONTENT_TYPE_DOTNET_SERIALIZED_OBJECT };
                    var formatter = new BinaryFormatter();
                    var stream = new MemoryStream(512);

                    // TODO: don't disable this warning! https://aka.ms/binaryformatter
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                    formatter.Serialize(stream, request);
                    var bytes = stream.ToArray();
                    var reply = template.SendAndReceive(Message.Create(bytes, messageHeaders.MessageHeaders));
                    stream = new MemoryStream((byte[])reply.Payload);
                    var obj = formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                    results.TryAdd(request, obj);
                }));
            }

            var receiveCount = new AtomicInteger();
            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            do
            {
                template.ReceiveAndReply<double, double>((payload) =>
                {
                    receiveCount.IncrementAndGet();
                    return payload * 3;
                });
                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > start + 20000)
                {
                    throw new Exception("Something wrong with RabbitMQ");
                }
            }
            while (receiveCount.Value < count * 2);

            Task.WaitAll(tasks.ToArray());
            container.Stop().Wait();
            Assert.Equal(count * 2, results.Count);

            foreach (var entry in results)
            {
                Assert.Equal(entry.Value, entry.Key * 3);
            }

            var messageId = Guid.NewGuid().ToString();
            var messageProperties = new RabbitHeaderAccessor
            {
                MessageId = messageId,
                ContentType = MessageHeaders.CONTENT_TYPE_TEXT_PLAIN,
                ReplyTo = REPLY_QUEUE_NAME
            };
            template.Send(Message.Create(Encoding.UTF8.GetBytes("test"), messageProperties.MessageHeaders));
            template.ReceiveAndReply<string, string>((str) => str.ToUpper());

            this.template.ReceiveTimeout = 20000;
            var result = this.template.Receive(REPLY_QUEUE_NAME);
            Assert.NotNull(result);
            Assert.Equal("TEST", Encoding.UTF8.GetString((byte[])result.Payload));
            Assert.Equal(messageId, result.Headers.CorrelationId());
            template.Stop().Wait();
        }

        [Fact]
        public void TestSendAndReceiveFastImplicit()
        {
            SendAndReceiveFastGuts(false, false, false);
        }

        [Fact]
        public void TestSendAndReceiveFastExplicit()
        {
            SendAndReceiveFastGuts(false, true, false);
        }

        [Fact]
        public void TestSendAndReceiveNeverFast()
        {
            SendAndReceiveFastGuts(true, false, true);
        }

        [Fact]
        public void TestSendAndReceiveNeverFastWitReplyQueue()
        {
            SendAndReceiveFastGuts(true, true, false);
        }

        [Fact]
        public void TestReplyCompressionWithContainer()
        {
            var container = new DirectMessageListenerContainer();
            container.ConnectionFactory = this.template.ConnectionFactory;
            container.SetQueueNames(ROUTE);
            var messageListener = new MessageListenerAdapter(null, new TestMessageHandlerString());
            messageListener.SetBeforeSendReplyPostProcessors(new GZipPostProcessor());
            container.MessageListener = messageListener;
            container.Initialize();
            container.Start().Wait();
            var template = CreateSendAndReceiveRabbitTemplate(this.template.ConnectionFactory);
            try
            {
                var props = new RabbitHeaderAccessor { ContentType = "text/plain" };
                var message = Message.Create(Encoding.UTF8.GetBytes("foo"), props.MessageHeaders);
                var reply = template.SendAndReceive(string.Empty, ROUTE, message);
                Assert.NotNull(reply);
                Assert.Equal("gzip:utf-8", reply.Headers.ContentEncoding());
                var unzipper = new GUnzipPostProcessor();
                reply = unzipper.PostProcessMessage(reply);
                Assert.Equal("FOO", Encoding.UTF8.GetString((byte[])reply.Payload));
            }
            finally
            {
                template.Stop().Wait();
                container.Stop().Wait();
            }
        }

        [Fact(Skip = "Requires expression language")]
        public void TestRouting()
        {
            var connection1 = new Mock<IConnection>();
            var channel1 = new Mock<RC.IModel>();
            cf1.Setup(f => f.CreateConnection()).Returns(connection1.Object);
            connection1.Setup(c => c.CreateChannel(false)).Returns(channel1.Object);
            connection1.Setup(c => c.IsOpen).Returns(true);
            channel1.Setup(c => c.IsOpen).Returns(true);

            var testPP = new TestPostProcessor("foo");
            routingTemplate.ConvertAndSend("exchange", "routingKey", "xyz", testPP);
            channel1.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));

            var connection2 = new Mock<IConnection>();
            var channel2 = new Mock<RC.IModel>();
            cf2.Setup(f => f.CreateConnection()).Returns(connection2.Object);
            connection2.Setup(c => c.CreateChannel(false)).Returns(channel2.Object);
            connection2.Setup(c => c.IsOpen).Returns(true);
            channel2.Setup(c => c.IsOpen).Returns(true);
            var testPP2 = new TestPostProcessor("bar");
            routingTemplate.ConvertAndSend("exchange", "routingKey", "xyz", testPP2);
            channel1.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));
        }

        [Fact]
        public void TestSendInGlobalTransactionCommit()
        {
            TestSendInGlobalTransactionGuts(false);
            var result = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("message", result);
            Assert.Null(template.Receive(ROUTE));
        }

        [Fact]
        public void TestSendInGlobalTransactionRollback()
        {
            TestSendInGlobalTransactionGuts(true);
            Assert.Null(template.Receive(ROUTE));
        }

        [Fact]
        public void TestSendToMissingExchange()
        {
            var shutdownLatch = new CountdownEvent(1);
            var shutdown = new AtomicReference<ShutdownSignalException>();
            var testListener = new TestChannelListener(shutdown, shutdownLatch);
            connectionFactory.AddChannelListener(testListener);

            var connLatch = new CountdownEvent(1);
            var testListener2 = new TestConnectionListener(shutdown, connLatch);
            connectionFactory.AddConnectionListener(testListener2);

            template.ConvertAndSend(Guid.NewGuid().ToString(), "foo", "bar");
            Assert.True(shutdownLatch.Wait(TimeSpan.FromSeconds(10)));
            template.IsChannelTransacted = true;
            try
            {
                template.ConvertAndSend(Guid.NewGuid().ToString(), "foo", "bar");
                throw new Exception("Expected exception");
            }
            catch (RabbitException)
            {
                var shutdownArgs = shutdown.Value.Args;
                Assert.Equal(60, shutdownArgs.ClassId);
                Assert.Equal(40, shutdownArgs.MethodId);
                Assert.Equal(404, shutdownArgs.ReplyCode);
                Assert.Contains("NOT_FOUND", shutdownArgs.ReplyText);
            }

            var signal = new RC.ShutdownEventArgs(RC.ShutdownInitiator.Library, 320, "CONNECTION_FORCED", 10, 0);
            connectionFactory.ConnectionShutdownCompleted(this, signal);

            Assert.True(connLatch.Wait(TimeSpan.FromSeconds(10)));
            Assert.Equal(10, shutdown.Value.Args.ClassId);
            Assert.Contains("CONNECTION_FORCED", shutdown.Value.Args.ReplyText);
            Assert.Equal(320, shutdown.Value.Args.ReplyCode);
        }

        [Fact]
        public void TestInvoke()
        {
            template.Invoke<object>(t =>
            {
                t.Execute<object>(c =>
                {
                    t.Execute<object>(chan =>
                    {
                        Assert.Same(c, chan);
                        return null;
                    });
                    return null;
                });
                return null;
            });
            Assert.Null(template._dedicatedChannels.Value);
        }

        [Fact]
        public void WaitForConfirms()
        {
            connectionFactory.PublisherConfirmType = CachingConnectionFactory.ConfirmType.CORRELATED;
            var messages = new List<string> { "foo", "bar" };
            var result = template.Invoke(t =>
            {
                messages.ForEach(m => t.ConvertAndSend(string.Empty, ROUTE, m));
                t.WaitForConfirmsOrDie(10_000);
                return true;
            });
            Assert.True(result);
        }

        protected virtual RabbitTemplate CreateSendAndReceiveRabbitTemplate(IConnectionFactory connectionFactory)
        {
            var template = new RabbitTemplate(connectionFactory)
            {
                UseDirectReplyToContainer = false
            };
            return template;
        }

        private void TestSendInGlobalTransactionGuts(bool rollback)
        {
            template.IsChannelTransacted = true;
            var tt = new TransactionTemplate(new TestTransactionManager());
            tt.Execute(status =>
            {
                template.ConvertAndSend(ROUTE, "message");
                if (rollback)
                {
                    var adapter = new TestTransactionSynchronizationAdapter();
                    TransactionSynchronizationManager.RegisterSynchronization(adapter);
                }
            });
        }

        private void SendAndReceiveFastGuts(bool tempQueue, bool setDirectReplyToExplicitly, bool expectUsedTemp)
        {
            var template = CreateSendAndReceiveRabbitTemplate(connectionFactory);
            try
            {
                template.Execute(channel =>
                {
                    channel.QueueDeclarePassive(Address.AMQ_RABBITMQ_REPLY_TO);
                });

                template.UseTemporaryReplyQueues = tempQueue;
                if (setDirectReplyToExplicitly)
                {
                    template.ReplyAddress = Address.AMQ_RABBITMQ_REPLY_TO;
                }

                var container = new DirectMessageListenerContainer();
                container.ConnectionFactory = template.ConnectionFactory;
                container.SetQueueNames(ROUTE);
                var replyToWas = new AtomicReference<string>();
                var delgate = new TestMessageHandler(replyToWas);
                var messageListenerAdapter = new MessageListenerAdapter(null, delgate) { MessageConverter = null };
                container.MessageListener = messageListenerAdapter;
                container.Start().Wait();
                template.DefaultReceiveQueue = ROUTE;
                template.RoutingKey = ROUTE;
                var result = template.ConvertSendAndReceive<string>("foo");
                container.Stop().Wait();
                Assert.Equal("FOO", result);
                if (expectUsedTemp)
                {
                    Assert.False(replyToWas.Value.StartsWith(Address.AMQ_RABBITMQ_REPLY_TO));
                }
                else
                {
                    Assert.StartsWith(Address.AMQ_RABBITMQ_REPLY_TO, replyToWas.Value);
                }
            }
            catch (Exception e)
            {
                Assert.Contains("404", e.InnerException.InnerException.Message);
            }
            finally
            {
                template.Stop().Wait();
            }
        }

        private void TestReceiveAndReply(int timeout)
        {
            template.DefaultReceiveQueue = ROUTE;
            template.RoutingKey = ROUTE;
            template.ConvertAndSend(ROUTE, "test");
            template.ReceiveTimeout = timeout;

            var received = ReceiveAndReply();
            var n = 0;
            while (timeout == 0 && !received && n++ < 100)
            {
                Thread.Sleep(100);
                received = ReceiveAndReply();
            }

            Assert.True(received);

            var receive = template.Receive();
            Assert.NotNull(receive);
            Assert.Equal("bar", receive.Headers.Get<string>("foo"));

            template.ConvertAndSend(ROUTE, 1);
            received = template.ReceiveAndReply<int, int>(ROUTE, (payload) => payload + 1);
            Assert.True(received);

            var result = template.ReceiveAndConvert<int>(ROUTE);
            Assert.Equal(2, result);

            template.ConvertAndSend(ROUTE, 2);
            received = template.ReceiveAndReply<int, int>(ROUTE, (payload) => payload * 2);
            Assert.True(received);

            result = template.ReceiveAndConvert<int>(ROUTE);
            Assert.Equal(4, result);

            received = false;
            if (timeout > 0)
            {
                template.ReceiveTimeout = 1;
            }

            try
            {
                received = template.ReceiveAndReply<IMessage, IMessage>((message) => message);
            }
            catch (ConsumeOkNotReceivedException)
            {
                // we're expecting no result, this could happen, depending on timing.
            }

            Assert.False(received);

            template.ConvertAndSend(ROUTE, "test");
            template.ReceiveTimeout = timeout;
            received = template.ReceiveAndReply<IMessage, IMessage>(message => null);
            Assert.True(received);

            template.ReceiveTimeout = 0;
            var result2 = template.Receive();
            Assert.Null(result2);

            template.ConvertAndSend(ROUTE, "TEST");
            template.ReceiveTimeout = timeout;
            received = template.ReceiveAndReply<IMessage, IMessage>(
                message =>
            {
                var messageProperties = new RabbitHeaderAccessor(new MessageHeaders()) { ContentType = message.Headers.ContentType() };
                messageProperties.SetHeader("testReplyTo", new Address(string.Empty, ROUTE));
                return Message.Create(message.Payload, messageProperties.MessageHeaders, message.Payload.GetType());
            },
                (request, reply) => reply.Headers.Get<Address>("testReplyTo"));

            Assert.True(received);
            var result3 = template.ReceiveAndConvert<string>(ROUTE);
            Assert.Equal("TEST", result3);

            template.ReceiveTimeout = 0;
            Assert.Null(template.Receive(ROUTE));

            template.IsChannelTransacted = true;

            template.ConvertAndSend(ROUTE, "TEST");
            template.ReceiveTimeout = timeout;
            var payloadReference = new AtomicReference<string>();
            var ttemplate = new TransactionTemplate(new TestTransactionManager());
            var result4 = ttemplate.Execute((status) =>
            {
                var received1 = template.ReceiveAndReply<string, object>(
                    payload =>
                    {
                        payloadReference.Value = payload;
                        return null;
                    });
                Assert.True(received1);
                return payloadReference.Value;
            });

            Assert.Equal("TEST", result4);
            template.ReceiveTimeout = 0;
            Assert.Null(template.Receive(ROUTE));

            template.ConvertAndSend(ROUTE, "TEST");
            template.ReceiveTimeout = timeout;

            try
            {
                ttemplate = new TransactionTemplate(new TestTransactionManager());
                ttemplate.Execute((status) =>
                {
                    template.ReceiveAndReply<IMessage, IMessage>(
                        message => message,
                        (request, reply) => throw new PlannedException());
                });
            }
            catch (Exception e)
            {
                Assert.IsType<PlannedException>(e.InnerException);
            }

            Assert.Equal("TEST", template.ReceiveAndConvert<string>(ROUTE));
            template.ReceiveTimeout = 0;
            Assert.Null(template.ReceiveAndConvert<string>(ROUTE));

            template.ConvertAndSend("test");
            template.ReceiveTimeout = timeout;
            try
            {
                template.ReceiveAndReply<double, object>(input => null);
                throw new Exception("Should have throw Exception");
            }
            catch (Exception e)
            {
                Assert.IsType<ArgumentException>(e.InnerException);
            }
        }

        private bool ReceiveAndReply()
        {
            return template.ReceiveAndReply<IMessage, IMessage>((message) =>
            {
                RabbitHeaderAccessor.GetMutableAccessor(message).SetHeader("foo", "bar");
                return message;
            });
        }

        private class TestConnectionListener : IConnectionListener
        {
            private AtomicReference<ShutdownSignalException> shutdown;
            private CountdownEvent connLatch;

            public TestConnectionListener(AtomicReference<ShutdownSignalException> shutdown, CountdownEvent connLatch)
            {
                this.shutdown = shutdown;
                this.connLatch = connLatch;
            }

            public void OnClose(IConnection connection)
            {
            }

            public void OnCreate(IConnection connection)
            {
            }

            public void OnShutDown(RC.ShutdownEventArgs args)
            {
                shutdown.Value = new ShutdownSignalException(args);
                connLatch.Signal();
            }
        }

        private class TestChannelListener : IChannelListener
        {
            private AtomicReference<ShutdownSignalException> shutdown;
            private CountdownEvent shutdownLatch;

            public TestChannelListener(AtomicReference<ShutdownSignalException> shutdown, CountdownEvent shutdownLatch)
            {
                this.shutdown = shutdown;
                this.shutdownLatch = shutdownLatch;
            }

            public void OnCreate(RC.IModel channel, bool transactional)
            {
            }

            public void OnShutDown(RC.ShutdownEventArgs args)
            {
                shutdown.Value = new ShutdownSignalException(args);
                shutdownLatch.Signal();
            }
        }

        private class TestTransactionSynchronizationAdapter : ITransactionSynchronization
        {
            public void AfterCommit()
            {
                TransactionSynchronizationUtils
                            .TriggerAfterCompletion(AbstractTransactionSynchronization.STATUS_ROLLED_BACK);
            }

            public void AfterCompletion(int status)
            {
            }

            public void BeforeCommit(bool readOnly)
            {
            }

            public void BeforeCompletion()
            {
            }

            public void Flush()
            {
            }

            public void Resume()
            {
            }

            public void Suspend()
            {
            }
        }

        private class TestPostProcessor : IMessagePostProcessor
        {
            public TestPostProcessor(string keyValue)
            {
                KeyValue = keyValue;
            }

            public string KeyValue { get; }

            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
                accessor.SetHeader("cfKey", KeyValue);
                return message;
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                return PostProcessMessage(message, null);
            }
        }

        private class TestMessageHandlerString
        {
            public string HandleMessage(string message)
            {
                return message.ToUpper();
            }
        }

        private class TestMessageHandler
        {
            private AtomicReference<string> replyToWas;

            public TestMessageHandler(AtomicReference<string> replyToWas)
            {
                this.replyToWas = replyToWas;
            }

            public IMessage HandleMessage(IMessage message)
            {
                replyToWas.Value = message.Headers.ReplyTo();
                return Message.Create(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString((byte[])message.Payload).ToUpper()), message.Headers);
            }
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
            public override string ToString()
            {
                return "FooAsAString";
            }
        }

        private class PostProcessor3 : IMessagePostProcessor
        {
            public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
            {
                return PostProcessMessage(message);
            }

            public IMessage PostProcessMessage(IMessage message)
            {
                try
                {
                    var newPayload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString((byte[])message.Payload).ToUpper());
                    return Message.Create(newPayload, message.Headers);
                }
                catch (Exception e)
                {
                    throw new RabbitException("unexpected failure in test", e);
                }
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
                var strings = message.Headers.Get<object>("strings") as List<object>;
                Assert.NotNull(strings);
                Assert.Contains("1", strings);
                Assert.Contains("2", strings);
                var objects = message.Headers.Get<object>("objects") as List<object>;
                Assert.NotNull(objects);
                Assert.Equal("FooAsAString", objects[0]);
                Assert.Equal("FooAsAString", objects[1]);
                var asObjects = message.Headers.Get<object>("bytes") as List<object>;
                var bytes = asObjects.Cast<byte>().ToArray();
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
                accessor.SetHeader("strings", new[] { "1", "2" });
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

            public override IConnection CreateConnection()
            {
                var dele = base.CreateConnection();
                return new MockConnection(dele);
            }
        }

        private class MockConnection : IConnection
        {
            public MockConnection(IConnection deleg)
            {
                Delegate = deleg;
            }

            public IConnection Delegate { get; }

            public bool IsOpen => Delegate.IsOpen;

            public int LocalPort => Delegate.LocalPort;

            public RC.IConnection Connection => Delegate.Connection;

            public void AddBlockedListener(IBlockedListener listener)
            {
                Delegate.AddBlockedListener(listener);
            }

            public void Close()
            {
                Delegate.Close();
            }

            public RC.IModel CreateChannel(bool transactional = false)
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

        private class MockConsumer : RC.IBasicConsumer
        {
            public MockConsumer(RC.IBasicConsumer deleg)
            {
                Delegate = deleg;
            }

            public RC.IBasicConsumer Delegate { get; }

            public RC.IModel Model => Delegate.Model;

            public event EventHandler<RC.Events.ConsumerEventArgs> ConsumerCancelled
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

            public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, RC.IBasicProperties properties, byte[] body)
            {
                Delegate.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
            }

            public void HandleModelShutdown(object model, RC.ShutdownEventArgs reason)
            {
                throw new NotImplementedException();
            }
        }

        private class MockChannel : PublisherCallbackChannel
        {
            public MockChannel(RC.IModel channel, ILogger logger = null)
                : base(channel, logger)
            {
            }

            public override string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, RC.IBasicConsumer consumer)
            {
                return base.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, new MockConsumer(consumer));
            }
        }
    }
}
