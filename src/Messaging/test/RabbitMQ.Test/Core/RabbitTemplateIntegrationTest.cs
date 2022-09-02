// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Steeltoe.Messaging.RabbitMQ.Support.PostProcessor;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[Trait("Category", "Integration")]
public abstract class RabbitTemplateIntegrationTest : IDisposable
{
    public const string Route = "test.queue.RabbitTemplateIntegrationTests";
    public const string ReplyQueueName = "test.reply.queue.RabbitTemplateIntegrationTests";

    private readonly CachingConnectionFactory _connectionFactory;

    protected RabbitTemplate Template { get; private set; }
    protected RabbitTemplate RoutingTemplate { get; }
    protected RabbitAdmin Admin { get; }
    protected Mock<IConnectionFactory> ConnectionFactory1 { get; }
    protected Mock<IConnectionFactory> ConnectionFactory2 { get; }
    protected Mock<IConnectionFactory> DefaultConnectionFactory { get; }

    protected RabbitTemplateIntegrationTest()
    {
        _connectionFactory = new CachingConnectionFactory("localhost")
        {
            IsPublisherReturns = true
        };

        Template = new RabbitTemplate(_connectionFactory)
        {
            ReplyTimeout = 10000
        };

        // template.SetSendConnectionFactorySelectorExpression(new LiteralExpression("foo"));
        var adminCf = new CachingConnectionFactory("localhost");
        Admin = new RabbitAdmin(adminCf);
        Admin.DeclareQueue(new Queue(Route));
        Admin.DeclareQueue(new Queue(ReplyQueueName));

        RoutingTemplate = new RabbitTemplate();

        // TODO: Requires expression language support
        // routingTemplate.SendConnectionFactorySelectorExpression = "messageProperties.headers['cfKey']"
        var routingConnFactory = new SimpleRoutingConnectionFactory();
        ConnectionFactory1 = new Mock<IConnectionFactory>();
        ConnectionFactory2 = new Mock<IConnectionFactory>();
        DefaultConnectionFactory = new Mock<IConnectionFactory>();
        routingConnFactory.AddTargetConnectionFactory("foo", ConnectionFactory1.Object);
        routingConnFactory.AddTargetConnectionFactory("bar", ConnectionFactory2.Object);
        RoutingTemplate.ConnectionFactory = routingConnFactory;
    }

    [Fact]
    public void TestChannelCloseInTx()
    {
        _connectionFactory.IsPublisherReturns = false;
        RC.IModel channel = _connectionFactory.CreateConnection().CreateChannel(true);
        var holder = new RabbitResourceHolder(channel, true);
        TransactionSynchronizationManager.BindResource(_connectionFactory, holder);

        try
        {
            Template.IsChannelTransacted = true;
            Template.ConvertAndSend(Route, "foo");
            Template.ConvertAndSend(Guid.NewGuid().ToString(), Route, "xxx");
            int n = 0;

            while (n++ < 100 && channel.IsOpen)
            {
                Thread.Sleep(100);
            }

            Assert.False(channel.IsOpen);

            try
            {
                Template.ConvertAndSend(Route, "bar");
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
                Assert.IsType<AlreadyClosedException>(e.InnerException);
            }
        }
        finally
        {
            TransactionSynchronizationManager.UnbindResource(_connectionFactory);
            channel.Close();
        }
    }

    [Fact]
    public void TestTemplateUsesPublisherConnectionUnlessInTx()
    {
        _connectionFactory.Destroy();
        Template.UsePublisherConnection = true;
        Template.ConvertAndSend("dummy", "foo");
        Assert.Null(_connectionFactory.Connection.Target);
        Assert.NotNull(((CachingConnectionFactory)_connectionFactory.PublisherConnectionFactory).Connection.Target);
        _connectionFactory.Destroy();
        Assert.Null(_connectionFactory.Connection.Target);
        Assert.Null(((CachingConnectionFactory)_connectionFactory.PublisherConnectionFactory).Connection.Target);
        RC.IModel channel = _connectionFactory.CreateConnection().CreateChannel(true);
        Assert.NotNull(_connectionFactory.Connection.Target);
        var holder = new RabbitResourceHolder(channel, true);
        TransactionSynchronizationManager.BindResource(_connectionFactory, holder);

        try
        {
            Template.IsChannelTransacted = true;
            Template.ConvertAndSend("dummy", "foo");
            Assert.NotNull(_connectionFactory.Connection.Target);
            Assert.Null(((CachingConnectionFactory)_connectionFactory.PublisherConnectionFactory).Connection.Target);
        }
        finally
        {
            TransactionSynchronizationManager.UnbindResource(_connectionFactory);
            channel.Close();
        }
    }

    [Fact]
    public void TestReceiveNonBlocking()
    {
        Template.ConvertAndSend(Route, "nonblock");
        int n = 0;
        string o = Template.ReceiveAndConvert<string>(Route);

        while (n++ < 100 && o == null)
        {
            Thread.Sleep(100);
            o = Template.ReceiveAndConvert<string>(Route);
        }

        Assert.NotNull(o);
        Assert.Equal("nonblock", o);
        Assert.Null(Template.Receive(Route));
    }

    [Fact]
    public void TestReceiveConsumerCanceled()
    {
        using var connectionFactory = new MockSingleConnectionFactory("localhost");

        Template = new RabbitTemplate(connectionFactory)
        {
            ReceiveTimeout = 10000
        };

        Assert.Throws<ConsumerCancelledException>(() => Template.Receive(Route));
    }

    [Fact]
    public void TestReceiveBlocking()
    {
        // TODO: this.template.setUserIdExpressionString("@cf.username");
        Template.ConvertAndSend(Route, "block");
        IMessage received = Template.Receive(Route, 10000);
        Assert.NotNull(received);
        Assert.Equal("block", EncodingUtils.Utf8.GetString((byte[])received.Payload));

        // TODO: assertThat(received.getMessageProperties().getReceivedUserId()).isEqualTo("guest");
        Template.ReceiveTimeout = 0;
        Assert.Null(Template.Receive(Route));
    }

    [Fact]
    public void TestReceiveBlockingNoTimeout()
    {
        Template.ConvertAndSend(Route, "blockNoTO");
        string o = Template.ReceiveAndConvert<string>(Route, -1);
        Assert.NotNull(o);
        Assert.Equal("blockNoTO", o);
        Template.ReceiveTimeout = 1; // test the no message after timeout path

        try
        {
            Assert.Null(Template.Receive(Route));
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
            Assert.Null(Template.ReceiveAndConvert<string>(Route, 10));
        }
        catch (ConsumeOkNotReceivedException)
        {
            // empty - race for consumeOk
        }

        Assert.Empty(_connectionFactory.CachedChannelsNonTransactional);
    }

    [Fact]
    public void TestReceiveBlockingTx()
    {
        Template.ConvertAndSend(Route, "blockTX");
        Template.IsChannelTransacted = true;
        Template.ReceiveTimeout = 10000;
        string o = Template.ReceiveAndConvert<string>(Route);
        Assert.NotNull(o);
        Assert.Equal("blockTX", o);
        Template.ReceiveTimeout = 0;
        Assert.Null(Template.Receive(Route));
    }

    [Fact]
    public void TestReceiveBlockingGlobalTx()
    {
        Template.ConvertAndSend(Route, "blockGTXNoTO");
        RabbitResourceHolder resourceHolder = ConnectionFactoryUtils.GetTransactionalResourceHolder(Template.ConnectionFactory, true);
        TransactionSynchronizationManager.SetActualTransactionActive(true);
        ConnectionFactoryUtils.BindResourceToTransaction(resourceHolder, Template.ConnectionFactory, true);
        Template.ReceiveTimeout = -1;
        Template.IsChannelTransacted = true;
        string o = Template.ReceiveAndConvert<string>(Route);
        resourceHolder.CommitAll();
        resourceHolder.CloseAll();
        Assert.Same(resourceHolder, TransactionSynchronizationManager.UnbindResource(Template.ConnectionFactory));
        Assert.NotNull(o);
        Assert.Equal("blockGTXNoTO", o);
        Template.ReceiveTimeout = 0;
        Assert.Null(Template.Receive(Route));
    }

    [Fact]
    public void TestSendToNonExistentAndThenReceive()
    {
        // If transacted then the commit fails on send, so we get a nice synchronous exception
        Template.IsChannelTransacted = true;

        try
        {
            Template.ConvertAndSend(string.Empty, "no.such.route", "message");

            // throw new Exception("Expected RabbitException");
        }
        catch (RabbitException)
        {
            // e.printStackTrace();
        }

        // Now send the real message, and all should be well...
        Template.ConvertAndSend(Route, "message");
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendAndReceiveWithPostProcessor()
    {
        Template.ConvertAndSend(Route, (object)"message", new PostProcessor1());
        Template.SetAfterReceivePostProcessors(new PostProcessor2());
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendAndReceive()
    {
        Template.ConvertAndSend(Route, "message");
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendAndReceiveUndeliverable()
    {
        Template.Mandatory = true;
        var ex = Assert.Throws<RabbitMessageReturnedException>(() => Template.ConvertSendAndReceive<string>($"{Route}xxxxxx", "undeliverable"));
        byte[] body = ex.ReturnedMessage.Payload as byte[];
        Assert.NotNull(body);
        Assert.Equal("undeliverable", EncodingUtils.Utf8.GetString(body));
        Assert.Contains(ex.ReplyText, "NO_ROUTE");
        Assert.Empty(Template.ReplyHolder);
    }

    [Fact]
    public void TestSendAndReceiveTransacted()
    {
        Template.IsChannelTransacted = true;
        Template.ConvertAndSend(Route, "message");
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendAndReceiveTransactedWithUncachedConnection()
    {
        var singleConnectionFactory = new SingleConnectionFactory("localhost");

        var rabbitTemplate = new RabbitTemplate(singleConnectionFactory)
        {
            IsChannelTransacted = true
        };

        rabbitTemplate.ConvertAndSend(Route, "message");
        string result = rabbitTemplate.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = rabbitTemplate.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
        singleConnectionFactory.Destroy();
    }

    [Fact]
    public void TestSendAndReceiveTransactedWithImplicitRollback()
    {
        Template.IsChannelTransacted = true;
        Template.ConvertAndSend(Route, "message");

        // Rollback of manual receive is implicit because the channel is
        // closed...
        var ex = Assert.Throws<RabbitUncategorizedException>(() => Template.Execute(c =>
        {
            c.BasicGet(Route, false);
            c.BasicRecover(true);
            throw new PlannedException();
        }));

        Assert.IsType<PlannedException>(ex.InnerException);
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendAndReceiveInCallback()
    {
        Template.ConvertAndSend(Route, "message");
        var messagePropertiesConverter = new DefaultMessageHeadersConverter();

        string result = Template.Execute(c =>
        {
            RC.BasicGetResult response = c.BasicGet(Route, false);

            IMessageHeaders props = messagePropertiesConverter.ToMessageHeaders(response.BasicProperties,
                new Envelope(response.DeliveryTag, response.Redelivered, response.Exchange, response.RoutingKey), EncodingUtils.Utf8);

            c.BasicAck(response.DeliveryTag, false);
            return new SimpleMessageConverter().FromMessage<string>(Message.Create(response.Body, props));
        });

        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestReceiveInExternalTransaction()
    {
        Template.ConvertAndSend(Route, "message");
        Template.IsChannelTransacted = true;
        string result = new TransactionTemplate(new TestTransactionManager()).Execute(_ => Template.ReceiveAndConvert<string>(Route));
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestReceiveInExternalTransactionWithRollback()
    {
        // Makes receive (and send in principle) transactional
        Template.IsChannelTransacted = true;
        Template.ConvertAndSend(Route, "message");

        Assert.Throws<PlannedException>(() =>
        {
            new TransactionTemplate(new TestTransactionManager()).Execute(_ =>
            {
                string result = Template.ReceiveAndConvert<string>(Route);
                Assert.NotNull(result);
                throw new PlannedException();
            });
        });

        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestReceiveInExternalTransactionWithNoRollback()
    {
        // Makes receive non-transactional
        Template.IsChannelTransacted = false;
        Template.ConvertAndSend(Route, "message");

        Assert.Throws<PlannedException>(() =>
        {
            new TransactionTemplate(new TestTransactionManager()).Execute(_ =>
            {
                Template.ReceiveAndConvert<string>(Route);
                throw new PlannedException();
            });
        });

        // No rollback
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendInExternalTransaction()
    {
        Template.IsChannelTransacted = true;

        new TransactionTemplate(new TestTransactionManager()).Execute(_ =>
        {
            Template.ConvertAndSend(Route, "message");
        });

        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestSendInExternalTransactionWithRollback()
    {
        // Makes receive non-transactional
        Template.IsChannelTransacted = true;

        Assert.Throws<PlannedException>(() =>
        {
            new TransactionTemplate(new TestTransactionManager()).Execute(_ =>
            {
                Template.ConvertAndSend(Route, "message");
                throw new PlannedException();
            });
        });

        // No rollback
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
    }

    [Fact]
    public void TestAtomicSendAndReceive()
    {
        using var cachingConnectionFactory = new CachingConnectionFactory("localhost");
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
        rabbitTemplate.DefaultSendDestination = new RabbitDestination(string.Empty, Route);
        rabbitTemplate.DefaultReceiveDestination = new RabbitDestination(Route);

        Task<IMessage> task = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = rabbitTemplate.Receive();

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            rabbitTemplate.Send(message.Headers.ReplyTo(), message);
            return message;
        });

        IMessage<byte[]> message = Message.Create(EncodingUtils.Utf8.GetBytes("test-message"), new MessageHeaders());
        rabbitTemplate.SendAndReceive(message);
        task.Wait(TimeSpan.FromSeconds(10));
        IMessage received = task.Result;
        Assert.NotNull(received);
    }

    [Fact]
    public void TestAtomicSendAndReceiveUserCorrelation()
    {
        using var cachingConnectionFactory = new CachingConnectionFactory("localhost");
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
        rabbitTemplate.DefaultSendDestination = new RabbitDestination(string.Empty, Route);
        rabbitTemplate.DefaultReceiveDestination = new RabbitDestination(Route);
        var remoteCorrelationId = new AtomicReference<string>();

        Task<IMessage> received = Task.Run(() =>
        {
            IMessage message = rabbitTemplate.Receive(10000);
            Assert.NotNull(message);
            remoteCorrelationId.Value = message.Headers.CorrelationId();
            rabbitTemplate.Send(message.Headers.ReplyTo(), message);
            return message;
        });

        var rabbitAdmin = new RabbitAdmin(cachingConnectionFactory);
        IQueue replyQueue = rabbitAdmin.DeclareQueue();
        rabbitTemplate.ReplyAddress = replyQueue.QueueName;
        rabbitTemplate.UserCorrelationId = true;
        rabbitTemplate.ReplyTimeout = 10000;
        var container = new DirectMessageListenerContainer(null, cachingConnectionFactory);
        container.SetQueues(replyQueue);
        container.MessageListener = rabbitTemplate;
        container.Initialize();
        container.StartAsync();

        var headers = new RabbitHeaderAccessor(new MessageHeaders())
        {
            CorrelationId = "myCorrelationId"
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("test-message"), headers.MessageHeaders);
        IMessage reply = rabbitTemplate.SendAndReceive(message);
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])received.Result.Payload));
        Assert.NotNull(reply);
        Assert.Equal("myCorrelationId", remoteCorrelationId.Value);
        Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])reply.Payload));
        reply = rabbitTemplate.Receive();
        Assert.Null(reply);
        rabbitTemplate.StopAsync().Wait();
        container.StopAsync().Wait();
        cachingConnectionFactory.Destroy();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithRoutingKey()
    {
        using var cachingConnectionFactory = new CachingConnectionFactory("localhost");
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);

        // Set up a consumer to respond to our producer
        Task<IMessage> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = rabbitTemplate.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            rabbitTemplate.Send(message.Headers.ReplyTo(), message);
            return message;
        });

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("test-message"), new MessageHeaders());
        IMessage reply = rabbitTemplate.SendAndReceive(Route, message);
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])received.Result.Payload));
        Assert.NotNull(reply);
        Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])reply.Payload));
        reply = rabbitTemplate.Receive(Route);
        Assert.Null(reply);
        rabbitTemplate.StopAsync().Wait();
        cachingConnectionFactory.Destroy();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithExchangeAndRoutingKey()
    {
        using var cachingConnectionFactory = new CachingConnectionFactory("localhost");
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);

        // Set up a consumer to respond to our producer
        Task<IMessage> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = rabbitTemplate.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            rabbitTemplate.Send(message.Headers.ReplyTo(), message);
            return message;
        });

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("test-message"), new MessageHeaders());
        IMessage reply = rabbitTemplate.SendAndReceive(string.Empty, Route, message);
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])received.Result.Payload));
        Assert.NotNull(reply);
        Assert.Equal("test-message", Encoding.UTF8.GetString((byte[])reply.Payload));
        reply = rabbitTemplate.Receive(Route);
        Assert.Null(reply);
        rabbitTemplate.StopAsync().Wait();
        cachingConnectionFactory.Destroy();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithConversion()
    {
        using var cachingConnectionFactory = new CachingConnectionFactory("localhost");
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
        rabbitTemplate.RoutingKey = Route;
        rabbitTemplate.DefaultReceiveQueue = Route;

        // Set up a consumer to respond to our producer
        Task<string> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = rabbitTemplate.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            rabbitTemplate.Send(message.Headers.ReplyTo(), message);
            return rabbitTemplate.MessageConverter.FromMessage<string>(message);
        });

        rabbitTemplate.ConvertSendAndReceive<string>("message");
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("message", received.Result);
        string result = rabbitTemplate.ReceiveAndConvert<string>();
        Assert.Null(result);
        rabbitTemplate.StopAsync().Wait();
        cachingConnectionFactory.Destroy();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithConversionUsingRoutingKey()
    {
        // Set up a consumer to respond to our producer
        Task<string> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = Template.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            Template.Send(message.Headers.ReplyTo(), message);
            return Template.MessageConverter.FromMessage<string>(message);
        });

        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(_connectionFactory);
        string result = rabbitTemplate.ConvertSendAndReceive<string>(Route, "message");
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("message", received.Result);
        Assert.Equal("message", result);

        result = rabbitTemplate.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
        rabbitTemplate.StopAsync().Wait();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithConversionUsingExchangeAndRoutingKey()
    {
        // Set up a consumer to respond to our producer
        Task<string> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = Template.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            Template.Send(message.Headers.ReplyTo(), message);
            return Template.MessageConverter.FromMessage<string>(message);
        });

        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(_connectionFactory);
        string result = rabbitTemplate.ConvertSendAndReceive<string>(string.Empty, Route, "message");
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("message", received.Result);
        Assert.Equal("message", result);

        result = rabbitTemplate.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
        rabbitTemplate.StopAsync().Wait();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessor()
    {
        using var cachingConnectionFactory = new CachingConnectionFactory("localhost");
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(cachingConnectionFactory);
        rabbitTemplate.RoutingKey = Route;
        rabbitTemplate.DefaultReceiveQueue = Route;

        // Set up a consumer to respond to our producer
        Task<string> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = rabbitTemplate.Receive();

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            rabbitTemplate.Send(message.Headers.ReplyTo(), message);
            return rabbitTemplate.MessageConverter.FromMessage<string>(message);
        });

        string result = rabbitTemplate.ConvertSendAndReceive<string>((object)"message", new PostProcessor3());
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("MESSAGE", received.Result);
        Assert.Equal("MESSAGE", result);

        result = rabbitTemplate.ReceiveAndConvert<string>();
        Assert.Null(result);
        rabbitTemplate.StopAsync().Wait();
        cachingConnectionFactory.Destroy();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessorUsingRoutingKey()
    {
        // Set up a consumer to respond to our producer
        Task<string> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = Template.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            Template.Send(message.Headers.ReplyTo(), message);
            return Template.MessageConverter.FromMessage<string>(message);
        });

        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(_connectionFactory);
        string result = rabbitTemplate.ConvertSendAndReceive<string>(Route, (object)"message", new PostProcessor3());
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("MESSAGE", received.Result);
        Assert.Equal("MESSAGE", result);

        result = rabbitTemplate.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
        rabbitTemplate.StopAsync().Wait();
    }

    [Fact]
    public void TestAtomicSendAndReceiveWithConversionAndMessagePostProcessorUsingExchangeAndRoutingKey()
    {
        // Set up a consumer to respond to our producer
        Task<string> received = Task.Run(() =>
        {
            IMessage message = null;

            for (int i = 0; i < 10; i++)
            {
                message = Template.Receive(Route);

                if (message != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            Assert.NotNull(message);
            Template.Send(message.Headers.ReplyTo(), message);
            return Template.MessageConverter.FromMessage<string>(message);
        });

        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(_connectionFactory);
        string result = rabbitTemplate.ConvertSendAndReceive<string>(string.Empty, Route, "message", new PostProcessor3());
        Assert.True(received.Wait(TimeSpan.FromSeconds(1)));
        Assert.Equal("MESSAGE", received.Result);
        Assert.Equal("MESSAGE", result);

        result = rabbitTemplate.ReceiveAndConvert<string>(Route);
        Assert.Null(result);
        rabbitTemplate.StopAsync().Wait();
    }

    [Fact]
    public void TestReceiveAndReplyNonStandardCorrelationNotBytes()
    {
        Template.DefaultReceiveQueue = Route;
        Template.RoutingKey = Route;

        var headers = new MessageHeaders(new Dictionary<string, object>
        {
            { "baz", "bar" }
        });

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), headers);
        Template.Send(Route, message);
        Template.CorrelationKey = "baz";
        bool received = Template.ReceiveAndReply<IMessage, IMessage>(_ => Message.Create(Encoding.UTF8.GetBytes("fuz"), new MessageHeaders()));
        Assert.True(received);
        IMessage message2 = Template.Receive();
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
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(_connectionFactory);
        rabbitTemplate.DefaultReceiveQueue = Route;
        rabbitTemplate.RoutingKey = Route;
        rabbitTemplate.ReplyAddress = ReplyQueueName;
        rabbitTemplate.ReplyTimeout = 20000;
        rabbitTemplate.ReceiveTimeout = 20000;

        var container = new DirectMessageListenerContainer();
        container.ConnectionFactory = rabbitTemplate.ConnectionFactory;
        container.SetQueueNames(ReplyQueueName);
        container.MessageListener = rabbitTemplate;
        container.StartAsync().Wait();

        const int count = 10;
        var results = new ConcurrentDictionary<double, object>();
        rabbitTemplate.CorrelationKey = "CorrelationKey";
        var tasks = new List<Task>();

        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var random = new Random();
                double request = random.NextDouble() * 100;
                object reply = rabbitTemplate.ConvertSendAndReceive<object>(request);
                results.TryAdd(request, reply);
            }));
        }

        for (int i = 0; i < count; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var random = new Random();
                double request = random.NextDouble() * 100;

                var messageHeaders = new RabbitHeaderAccessor(new MessageHeaders())
                {
                    ContentType = MessageHeaders.ContentTypeDotNetSerializedObject
                };

                var formatter = new BinaryFormatter();
                using var requestStream = new MemoryStream(512);

                // TODO: [BREAKING] Don't use binary serialization, it's insecure! https://aka.ms/binaryformatter
                // Tracked at: https://github.com/SteeltoeOSS/Steeltoe/issues/487.
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(requestStream, request);
                byte[] bytes = requestStream.ToArray();
                IMessage reply = rabbitTemplate.SendAndReceive(Message.Create(bytes, messageHeaders.MessageHeaders));

                using var replyStream = new MemoryStream((byte[])reply.Payload);
                object obj = formatter.Deserialize(replyStream);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                results.TryAdd(request, obj);
            }));
        }

        var receiveCount = new AtomicInteger();
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        do
        {
            rabbitTemplate.ReceiveAndReply<double, double>(payload =>
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
        container.StopAsync().Wait();
        Assert.Equal(count * 2, results.Count);

        foreach (KeyValuePair<double, object> entry in results)
        {
            Assert.Equal(entry.Value, entry.Key * 3);
        }

        string messageId = Guid.NewGuid().ToString();

        var messageProperties = new RabbitHeaderAccessor
        {
            MessageId = messageId,
            ContentType = MessageHeaders.ContentTypeTextPlain,
            ReplyTo = ReplyQueueName
        };

        rabbitTemplate.Send(Message.Create(Encoding.UTF8.GetBytes("test"), messageProperties.MessageHeaders));
        rabbitTemplate.ReceiveAndReply<string, string>(str => str.ToUpper());

        Template.ReceiveTimeout = 20000;
        IMessage result = Template.Receive(ReplyQueueName);
        Assert.NotNull(result);
        Assert.Equal("TEST", Encoding.UTF8.GetString((byte[])result.Payload));
        Assert.Equal(messageId, result.Headers.CorrelationId());
        rabbitTemplate.StopAsync().Wait();
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
        container.ConnectionFactory = Template.ConnectionFactory;
        container.SetQueueNames(Route);
        var messageListener = new MessageListenerAdapter(null, new TestMessageHandlerString());
        messageListener.SetBeforeSendReplyPostProcessors(new GZipPostProcessor());
        container.MessageListener = messageListener;
        container.Initialize();
        container.StartAsync().Wait();
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(Template.ConnectionFactory);

        try
        {
            var props = new RabbitHeaderAccessor
            {
                ContentType = "text/plain"
            };

            IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("foo"), props.MessageHeaders);
            IMessage reply = rabbitTemplate.SendAndReceive(string.Empty, Route, message);
            Assert.NotNull(reply);
            Assert.Equal("gzip:utf-8", reply.Headers.ContentEncoding());
            var unzipper = new GUnzipPostProcessor();
            reply = unzipper.PostProcessMessage(reply);
            Assert.Equal("FOO", Encoding.UTF8.GetString((byte[])reply.Payload));
        }
        finally
        {
            rabbitTemplate.StopAsync().Wait();
            container.StopAsync().Wait();
        }
    }

    [Fact(Skip = "Requires expression language")]
    public void TestRouting()
    {
        var connection1 = new Mock<IConnection>();
        var channel1 = new Mock<RC.IModel>();
        ConnectionFactory1.Setup(f => f.CreateConnection()).Returns(connection1.Object);
        connection1.Setup(c => c.CreateChannel(false)).Returns(channel1.Object);
        connection1.Setup(c => c.IsOpen).Returns(true);
        channel1.Setup(c => c.IsOpen).Returns(true);

        var testPp = new TestPostProcessor("foo");
        RoutingTemplate.ConvertAndSend("exchange", "routingKey", "xyz", testPp);
        channel1.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));

        var connection2 = new Mock<IConnection>();
        var channel2 = new Mock<RC.IModel>();
        ConnectionFactory2.Setup(f => f.CreateConnection()).Returns(connection2.Object);
        connection2.Setup(c => c.CreateChannel(false)).Returns(channel2.Object);
        connection2.Setup(c => c.IsOpen).Returns(true);
        channel2.Setup(c => c.IsOpen).Returns(true);
        var testPp2 = new TestPostProcessor("bar");
        RoutingTemplate.ConvertAndSend("exchange", "routingKey", "xyz", testPp2);
        channel1.Verify(c => c.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));
    }

    [Fact]
    public void TestSendInGlobalTransactionCommit()
    {
        TestSendInGlobalTransactionGuts(false);
        string result = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("message", result);
        Assert.Null(Template.Receive(Route));
    }

    [Fact]
    public void TestSendInGlobalTransactionRollback()
    {
        TestSendInGlobalTransactionGuts(true);
        Assert.Null(Template.Receive(Route));
    }

    [Fact]
    public void TestSendToMissingExchange()
    {
        var shutdownLatch = new CountdownEvent(1);
        var shutdown = new AtomicReference<ShutdownSignalException>();
        var testListener = new TestChannelListener(shutdown, shutdownLatch);
        _connectionFactory.AddChannelListener(testListener);

        var connLatch = new CountdownEvent(1);
        var testListener2 = new TestConnectionListener(shutdown, connLatch);
        _connectionFactory.AddConnectionListener(testListener2);

        Template.ConvertAndSend(Guid.NewGuid().ToString(), "foo", "bar");
        Assert.True(shutdownLatch.Wait(TimeSpan.FromSeconds(10)));
        Template.IsChannelTransacted = true;

        try
        {
            Template.ConvertAndSend(Guid.NewGuid().ToString(), "foo", "bar");
            throw new Exception("Expected exception");
        }
        catch (RabbitException)
        {
            RC.ShutdownEventArgs shutdownArgs = shutdown.Value.Args;
            Assert.Equal(60, shutdownArgs.ClassId);
            Assert.Equal(40, shutdownArgs.MethodId);
            Assert.Equal(404, shutdownArgs.ReplyCode);
            Assert.Contains("NOT_FOUND", shutdownArgs.ReplyText);
        }

        var signal = new RC.ShutdownEventArgs(RC.ShutdownInitiator.Library, 320, "CONNECTION_FORCED", 10, 0);
        _connectionFactory.ConnectionShutdownCompleted(this, signal);

        Assert.True(connLatch.Wait(TimeSpan.FromSeconds(10)));
        Assert.Equal(10, shutdown.Value.Args.ClassId);
        Assert.Contains("CONNECTION_FORCED", shutdown.Value.Args.ReplyText);
        Assert.Equal(320, shutdown.Value.Args.ReplyCode);
    }

    [Fact]
    public void TestInvoke()
    {
        Template.Invoke<object>(t =>
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

        Assert.Null(Template.DedicatedChannels.Value);
    }

    [Fact]
    public void WaitForConfirms()
    {
        _connectionFactory.PublisherConfirmType = CachingConnectionFactory.ConfirmType.Correlated;

        var messages = new List<string>
        {
            "foo",
            "bar"
        };

        bool result = Template.Invoke(t =>
        {
            messages.ForEach(m => t.ConvertAndSend(string.Empty, Route, m));
            t.WaitForConfirmsOrDie(10_000);
            return true;
        });

        Assert.True(result);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Admin.DeleteQueue(Route);
            Admin.DeleteQueue(ReplyQueueName);
            Admin.ConnectionFactory.Dispose();
            Template.Dispose();
            _connectionFactory.Dispose();
        }
    }

    protected virtual RabbitTemplate CreateSendAndReceiveRabbitTemplate(IConnectionFactory connectionFactory)
    {
        var rabbitTemplate = new RabbitTemplate(connectionFactory)
        {
            UseDirectReplyToContainer = false
        };

        return rabbitTemplate;
    }

    private void TestSendInGlobalTransactionGuts(bool rollback)
    {
        Template.IsChannelTransacted = true;
        var tt = new TransactionTemplate(new TestTransactionManager());

        tt.Execute(_ =>
        {
            Template.ConvertAndSend(Route, "message");

            if (rollback)
            {
                var adapter = new TestTransactionSynchronizationAdapter();
                TransactionSynchronizationManager.RegisterSynchronization(adapter);
            }
        });
    }

    private void SendAndReceiveFastGuts(bool tempQueue, bool setDirectReplyToExplicitly, bool expectUsedTemp)
    {
        using RabbitTemplate rabbitTemplate = CreateSendAndReceiveRabbitTemplate(_connectionFactory);

        try
        {
            rabbitTemplate.Execute(channel =>
            {
                channel.QueueDeclarePassive(Address.AmqRabbitMQReplyTo);
            });

            rabbitTemplate.UseTemporaryReplyQueues = tempQueue;

            if (setDirectReplyToExplicitly)
            {
                rabbitTemplate.ReplyAddress = Address.AmqRabbitMQReplyTo;
            }

            var container = new DirectMessageListenerContainer();
            container.ConnectionFactory = rabbitTemplate.ConnectionFactory;
            container.SetQueueNames(Route);
            var replyToWas = new AtomicReference<string>();
            var handler = new TestMessageHandler(replyToWas);

            var messageListenerAdapter = new MessageListenerAdapter(null, handler)
            {
                MessageConverter = null
            };

            container.MessageListener = messageListenerAdapter;
            container.StartAsync().Wait();
            rabbitTemplate.DefaultReceiveQueue = Route;
            rabbitTemplate.RoutingKey = Route;
            string result = rabbitTemplate.ConvertSendAndReceive<string>("foo");
            container.StopAsync().Wait();
            Assert.Equal("FOO", result);

            if (expectUsedTemp)
            {
                Assert.False(replyToWas.Value.StartsWith(Address.AmqRabbitMQReplyTo));
            }
            else
            {
                Assert.StartsWith(Address.AmqRabbitMQReplyTo, replyToWas.Value);
            }
        }
        catch (Exception e)
        {
            Assert.Contains("404", e.InnerException.InnerException.Message);
        }
        finally
        {
            rabbitTemplate.StopAsync().Wait();
        }
    }

    private void TestReceiveAndReply(int timeout)
    {
        Template.DefaultReceiveQueue = Route;
        Template.RoutingKey = Route;
        Template.ConvertAndSend(Route, "test");
        Template.ReceiveTimeout = timeout;

        bool received = ReceiveAndReply();
        int n = 0;

        while (timeout == 0 && !received && n++ < 100)
        {
            Thread.Sleep(100);
            received = ReceiveAndReply();
        }

        Assert.True(received);

        IMessage receive = Template.Receive();
        Assert.NotNull(receive);
        Assert.Equal("bar", receive.Headers.Get<string>("foo"));

        Template.ConvertAndSend(Route, 1);
        received = Template.ReceiveAndReply<int, int>(Route, payload => payload + 1);
        Assert.True(received);

        int result = Template.ReceiveAndConvert<int>(Route);
        Assert.Equal(2, result);

        Template.ConvertAndSend(Route, 2);
        received = Template.ReceiveAndReply<int, int>(Route, payload => payload * 2);
        Assert.True(received);

        result = Template.ReceiveAndConvert<int>(Route);
        Assert.Equal(4, result);

        received = false;

        if (timeout > 0)
        {
            Template.ReceiveTimeout = 1;
        }

        try
        {
            received = Template.ReceiveAndReply<IMessage, IMessage>(message => message);
        }
        catch (ConsumeOkNotReceivedException)
        {
            // we're expecting no result, this could happen, depending on timing.
        }

        Assert.False(received);

        Template.ConvertAndSend(Route, "test");
        Template.ReceiveTimeout = timeout;
        received = Template.ReceiveAndReply<IMessage, IMessage>(_ => null);
        Assert.True(received);

        Template.ReceiveTimeout = 0;
        IMessage result2 = Template.Receive();
        Assert.Null(result2);

        Template.ConvertAndSend(Route, "TEST");
        Template.ReceiveTimeout = timeout;

        received = Template.ReceiveAndReply<IMessage, IMessage>(message =>
        {
            var messageProperties = new RabbitHeaderAccessor(new MessageHeaders())
            {
                ContentType = message.Headers.ContentType()
            };

            messageProperties.SetHeader("testReplyTo", new Address(string.Empty, Route));
            return Message.Create(message.Payload, messageProperties.MessageHeaders, message.Payload.GetType());
        }, (_, reply) => reply.Headers.Get<Address>("testReplyTo"));

        Assert.True(received);
        string result3 = Template.ReceiveAndConvert<string>(Route);
        Assert.Equal("TEST", result3);

        Template.ReceiveTimeout = 0;
        Assert.Null(Template.Receive(Route));

        Template.IsChannelTransacted = true;

        Template.ConvertAndSend(Route, "TEST");
        Template.ReceiveTimeout = timeout;
        var payloadReference = new AtomicReference<string>();
        var transactionTemplate = new TransactionTemplate(new TestTransactionManager());

        string result4 = transactionTemplate.Execute(_ =>
        {
            bool received1 = Template.ReceiveAndReply<string, object>(payload =>
            {
                payloadReference.Value = payload;
                return null;
            });

            Assert.True(received1);
            return payloadReference.Value;
        });

        Assert.Equal("TEST", result4);
        Template.ReceiveTimeout = 0;
        Assert.Null(Template.Receive(Route));

        Template.ConvertAndSend(Route, "TEST");
        Template.ReceiveTimeout = timeout;

        try
        {
            transactionTemplate = new TransactionTemplate(new TestTransactionManager());

            transactionTemplate.Execute(_ =>
            {
                Template.ReceiveAndReply<IMessage, IMessage>(message => message, (_, _) => throw new PlannedException());
            });
        }
        catch (Exception e)
        {
            Assert.IsType<PlannedException>(e.InnerException);
        }

        Assert.Equal("TEST", Template.ReceiveAndConvert<string>(Route));
        Template.ReceiveTimeout = 0;
        Assert.Null(Template.ReceiveAndConvert<string>(Route));

        Template.ConvertAndSend("test");
        Template.ReceiveTimeout = timeout;

        try
        {
            Template.ReceiveAndReply<double, object>(_ => null);
            throw new Exception("Should have throw Exception");
        }
        catch (Exception e)
        {
            Assert.IsType<InvalidOperationException>(e.InnerException);
        }
    }

    private bool ReceiveAndReply()
    {
        return Template.ReceiveAndReply<IMessage, IMessage>(message =>
        {
            RabbitHeaderAccessor.GetMutableAccessor(message).SetHeader("foo", "bar");
            return message;
        });
    }

    private sealed class TestConnectionListener : IConnectionListener
    {
        private readonly AtomicReference<ShutdownSignalException> _shutdown;
        private readonly CountdownEvent _connLatch;

        public TestConnectionListener(AtomicReference<ShutdownSignalException> shutdown, CountdownEvent connLatch)
        {
            _shutdown = shutdown;
            _connLatch = connLatch;
        }

        public void OnClose(IConnection connection)
        {
        }

        public void OnCreate(IConnection connection)
        {
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
            _shutdown.Value = new ShutdownSignalException(args);
            _connLatch.Signal();
        }
    }

    private sealed class TestChannelListener : IChannelListener
    {
        private readonly AtomicReference<ShutdownSignalException> _shutdown;
        private readonly CountdownEvent _shutdownLatch;

        public TestChannelListener(AtomicReference<ShutdownSignalException> shutdown, CountdownEvent shutdownLatch)
        {
            _shutdown = shutdown;
            _shutdownLatch = shutdownLatch;
        }

        public void OnCreate(RC.IModel channel, bool transactional)
        {
        }

        public void OnShutDown(RC.ShutdownEventArgs args)
        {
            _shutdown.Value = new ShutdownSignalException(args);
            _shutdownLatch.Signal();
        }
    }

    private sealed class TestTransactionSynchronizationAdapter : ITransactionSynchronization
    {
        public void AfterCommit()
        {
            TransactionSynchronizationUtils.TriggerAfterCompletion(AbstractTransactionSynchronization.StatusRolledBack);
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

    private sealed class TestPostProcessor : IMessagePostProcessor
    {
        public string KeyValue { get; }

        public TestPostProcessor(string keyValue)
        {
            KeyValue = keyValue;
        }

        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.SetHeader("cfKey", KeyValue);
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            return PostProcessMessage(message, null);
        }
    }

    private sealed class TestMessageHandlerString
    {
#pragma warning disable S1144 // Unused private types or members should be removed
        public string HandleMessage(string message)
        {
            return message.ToUpper();
        }
#pragma warning restore S1144 // Unused private types or members should be removed
    }

    private sealed class TestMessageHandler
    {
        private readonly AtomicReference<string> _replyToWas;

        public TestMessageHandler(AtomicReference<string> replyToWas)
        {
            _replyToWas = replyToWas;
        }

#pragma warning disable S1144 // Unused private types or members should be removed
        public IMessage HandleMessage(IMessage message)
        {
            _replyToWas.Value = message.Headers.ReplyTo();
            return Message.Create(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString((byte[])message.Payload).ToUpper()), message.Headers);
        }
#pragma warning restore S1144 // Unused private types or members should be removed
    }

    private sealed class TestTransactionManager : AbstractPlatformTransactionManager
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

    public sealed class PlannedException : Exception
    {
        public PlannedException()
            : base("Planned")
        {
        }
    }

    private sealed class Foo
    {
        public override string ToString()
        {
            return "FooAsAString";
        }
    }

    private sealed class PostProcessor3 : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return PostProcessMessage(message);
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            try
            {
                byte[] newPayload = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString((byte[])message.Payload).ToUpper());
                return Message.Create(newPayload, message.Headers);
            }
            catch (Exception e)
            {
                throw new RabbitException("unexpected failure in test", e);
            }
        }
    }

    private sealed class PostProcessor2 : IMessagePostProcessor
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
            byte[] bytes = asObjects.Cast<byte>().ToArray();
            Assert.Equal("abc", EncodingUtils.Utf8.GetString(bytes));
            return message;
        }
    }

    private sealed class PostProcessor1 : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return PostProcessMessage(message);
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.ContentType = "text/other";

            accessor.SetHeader("strings", new[]
            {
                "1",
                "2"
            });

            accessor.SetHeader("objects", new object[]
            {
                new Foo(),
                new Foo()
            });

            accessor.SetHeader("bytes", EncodingUtils.Utf8.GetBytes("abc"));
            return message;
        }
    }

    private sealed class MockSingleConnectionFactory : SingleConnectionFactory
    {
        public MockSingleConnectionFactory(string hostname)
            : base(hostname)
        {
        }

        public override IConnection CreateConnection()
        {
            IConnection connection = base.CreateConnection();
            return new MockConnection(connection);
        }
    }

    private sealed class MockConnection : IConnection
    {
        public IConnection Delegate { get; }

        public bool IsOpen => Delegate.IsOpen;

        public int LocalPort => Delegate.LocalPort;

        public RC.IConnection Connection => Delegate.Connection;

        public MockConnection(IConnection connection)
        {
            Delegate = connection;
        }

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
            RC.IModel chan = Delegate.CreateChannel(transactional);
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

    private sealed class MockConsumer : RC.IBasicConsumer
    {
        public RC.IBasicConsumer Delegate { get; }

        public RC.IModel Model => Delegate.Model;

        public event EventHandler<ConsumerEventArgs> ConsumerCancelled
        {
            add => Delegate.ConsumerCancelled += value;
            remove => Delegate.ConsumerCancelled -= value;
        }

        public MockConsumer(RC.IBasicConsumer consumer)
        {
            Delegate = consumer;
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

        public void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
            RC.IBasicProperties properties, byte[] body)
        {
            Delegate.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        }

        public void HandleModelShutdown(object model, RC.ShutdownEventArgs reason)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class MockChannel : PublisherCallbackChannel
    {
        public MockChannel(RC.IModel channel, ILogger logger = null)
            : base(channel, logger)
        {
        }

        public override string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments,
            RC.IBasicConsumer consumer)
        {
            return base.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, new MockConsumer(consumer));
        }
    }
}
