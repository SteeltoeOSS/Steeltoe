// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Transaction;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitTemplateTest
{
    [Fact]
    public void ReturnConnectionAfterCommit()
    {
        var txTemplate = new TransactionTemplate(new TestTransactionManager());
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

        var connectionFactory = new CachingConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            IsChannelTransacted = true
        };

        txTemplate.Execute(_ =>
        {
            template.ConvertAndSend("foo", "bar");
        });
        txTemplate.Execute(_ =>
        {
            template.ConvertAndSend("baz", "qux");
        });
        mockConnectionFactory.Verify(c => c.CreateConnection(It.IsAny<string>()), Times.Once);
        mockConnection.Verify(c => c.CreateModel(), Times.Once);
    }

    [Fact]
    public void TestConvertBytes()
    {
        var template = new RabbitTemplate();
        var payload = EncodingUtils.GetDefaultEncoding().GetBytes("Hello, world!");
        var message = template.ConvertMessageIfNecessary(payload);
        Assert.Same(payload, message.Payload);
    }

    [Fact]
    public void TestConvertString()
    {
        var template = new RabbitTemplate();
        var payload = "Hello, world!";
        var message = template.ConvertMessageIfNecessary(payload);
        var messageString = EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Payload);
        Assert.Equal(payload, messageString);
    }

    [Fact]
    public void TestConvertMessage()
    {
        var template = new RabbitTemplate();
        var payload = EncodingUtils.GetDefaultEncoding().GetBytes("Hello, world!");
        var input = Message.Create(payload, new MessageHeaders());
        var message = template.ConvertMessageIfNecessary(input);
        Assert.Same(message, input);
    }

    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void DoNotHangConsumerThread()
#pragma warning restore S2699 // Tests should include assertions
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

        var consumer = new AtomicReference<RC.IBasicConsumer>();
        mockChannel.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(new RC.QueueDeclareOk("foo", 0, 0));
        mockChannel.Setup(c => c.BasicConsume(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<RC.IBasicConsumer>()))
            .Callback<string, bool, string, bool, bool, IDictionary<string, object>, RC.IBasicConsumer>((_, _, _, _, _, _, arg7) => consumer.Value = arg7);
        var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            ReplyTimeout = 1
        };
        var payload = EncodingUtils.GetDefaultEncoding().GetBytes("Hello, world!");
        var input = Message.Create(payload, new MessageHeaders());
        template.DoSendAndReceiveWithTemporary("foo", "bar", input, null, default);

        // used to hang here because of the SynchronousQueue and doSendAndReceive() already exited
        consumer.Value.HandleBasicDeliver("foo", 1ul, false, "foo", "bar", new MockRabbitBasicProperties(), Array.Empty<byte>());
    }

    [Fact]
    public void TestRetry()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var count = new AtomicInteger();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Callback(() => count.IncrementAndGet())
            .Throws(new AuthenticationFailureException("foo"));

        var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            RetryTemplate = new PollyRetryTemplate(new Dictionary<Type, bool>(), 3, true, 1, 1, 1)
        };
        try
        {
            template.ConvertAndSend("foo", "bar", "baz");
        }
        catch (RabbitAuthenticationException e)
        {
            Assert.Contains("foo", e.InnerException.Message);
        }

        Assert.Equal(3, count.Value);
    }

    [Fact]
    public void TestEvaluateDirectReplyToWithConnectException()
    {
        var mockConnectionFactory = new Mock<IConnectionFactory>();
        mockConnectionFactory.Setup(f => f.CreateConnection())
            .Throws(new RabbitConnectException(null));
        var template = new RabbitTemplate(mockConnectionFactory.Object);
        Assert.Throws<RabbitConnectException>(() => template.ConvertSendAndReceive<object>("foo"));
        Assert.False(template.EvaluatedFastReplyTo);
    }

    [Fact]
    public void TestEvaluateDirectReplyToWithIOException()
    {
        var mockConnectionFactory = new Mock<IConnectionFactory>();
        mockConnectionFactory.Setup(f => f.CreateConnection())
            .Throws(new RabbitIOException(null));
        var template = new RabbitTemplate(mockConnectionFactory.Object);
        Assert.Throws<RabbitIOException>(() => template.ConvertSendAndReceive<object>("foo"));
        Assert.False(template.EvaluatedFastReplyTo);
    }

    [Fact]
    public void TestEvaluateDirectReplyToWithIOExceptionDeclareFailed()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);

        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
        mockChannel.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));
        mockChannel.Setup(c => c.QueueDeclarePassive(Address.AmqRabbitMQReplyTo)).Throws(new ShutdownSignalException(new RC.ShutdownEventArgs(RC.ShutdownInitiator.Peer, RabbitUtils.NotFound, string.Empty, RabbitUtils.QueueClassId, RabbitUtils.DeclareMethodId)));
        var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            ReplyTimeout = 1
        };
        template.ConvertSendAndReceive<object>("foo");
        Assert.True(template.EvaluatedFastReplyTo);
        Assert.False(template.UsingFastReplyTo);
    }

    [Fact]
    public void TestEvaluateDirectReplyToOk()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel = new Mock<RC.IModel>();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);

        mockChannel.Setup(c => c.IsOpen).Returns(true);
        mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
        mockChannel.Setup(c => c.QueueDeclarePassive(Address.AmqRabbitMQReplyTo))
            .Returns(() => new RC.QueueDeclareOk(Address.AmqRabbitMQReplyTo, 0, 0));
        var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            ReplyTimeout = 1
        };
        template.ConvertSendAndReceive<object>("foo");
        Assert.True(template.EvaluatedFastReplyTo);
        Assert.True(template.UsingFastReplyTo);
    }

    [Fact]
    public void TestRecovery()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var count = new AtomicInteger();
        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Callback(() => count.IncrementAndGet())
            .Throws(new AuthenticationFailureException("foo"));
        var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            RetryTemplate = new PollyRetryTemplate(new Dictionary<Type, bool>(), 3, true, 1, 1, 1)
        };

        var recoverInvoked = new AtomicBoolean();
        template.RecoveryCallback = new TestRecoveryRecoveryCallback(recoverInvoked);
        template.ConvertAndSend("foo", "bar", "baz");
        Assert.Equal(3, count.Value);
        Assert.True(recoverInvoked.Value);
    }

    [Fact]
    public void TestPublisherConfirmsReturnsSetup()
    {
        var mockConnectionFactory = new Mock<IConnectionFactory>();
        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IPublisherCallbackChannel>();
        mockChannel.Setup(m => m.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
        mockConnectionFactory.Setup(f => f.IsPublisherConfirms).Returns(true);
        mockConnectionFactory.Setup(f => f.IsPublisherReturns).Returns(true);
        mockConnectionFactory.Setup(f => f.CreateConnection())
            .Returns(mockConnection.Object);
        mockConnection.Setup(c => c.CreateChannel(false)).Returns(mockChannel.Object);
        var template = new RabbitTemplate(mockConnectionFactory.Object);
        template.ConvertAndSend("foo");
        mockChannel.Verify(c => c.AddListener(template));
    }

    [Fact]
    public void TestNoListenerAllowed1()
    {
        var template = new RabbitTemplate();
        Assert.Throws<InvalidOperationException>(() => template.GetExpectedQueueNames());
    }

    [Fact]
    public void TestNoListenerAllowed2()
    {
        var template = new RabbitTemplate
        {
            ReplyAddress = Address.AmqRabbitMQReplyTo
        };
        Assert.Throws<InvalidOperationException>(() => template.GetExpectedQueueNames());
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void TestRoutingConnectionFactory()
#pragma warning restore S2699 // Tests should include assertions
    {
        // TODO: Test this when expression language implemented
    }

    [Fact]
    public void TestNestedTxBinding()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();
        var mockChannel2 = new Mock<RC.IModel>();

        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockChannel2.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.IsOpen).Returns(true);

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.SetupSequence(c => c.CreateModel()).Returns(mockChannel1.Object).Returns(mockChannel2.Object);

        mockChannel1.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
        mockChannel2.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

        mockChannel1.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));
        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object);

        var rabbitTemplate = new RabbitTemplate(ccf)
        {
            IsChannelTransacted = true
        };
        var admin = new RabbitAdmin(rabbitTemplate);
        var mockContext = new Mock<IApplicationContext>();
        mockContext.Setup(c => c.GetServices<IQueue>()).Returns(new List<IQueue> { new Queue("foo") });
        admin.ApplicationContext = mockContext.Object;
        var templateChannel = new AtomicReference<RC.IModel>();
        var transTemplate = new TransactionTemplate(new TestTransactionManager());
        transTemplate.Execute(_ =>
        {
            return rabbitTemplate.Execute(c =>
            {
                templateChannel.Value = ((IChannelProxy)c).TargetChannel;
                return true;
            });
        });
        mockChannel1.Verify(c => c.TxSelect());
        mockChannel1.Verify(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()));
        mockChannel1.Verify(c => c.TxCommit());
        Assert.Same(templateChannel.Value, mockChannel1.Object);
    }

    [Fact]
    public void TestShutdownWhileWaitingForReply()
    {
        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();
        var mockConnection = new Mock<RC.IConnection>();
        var mockChannel1 = new Mock<RC.IModel>();

        mockChannel1.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.IsOpen).Returns(true);

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>())).Returns(mockConnection.Object);
        mockConnection.SetupSequence(c => c.CreateModel()).Returns(mockChannel1.Object);

        mockChannel1.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());

        mockChannel1.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));

        var listener = new AtomicReference<EventHandler<RC.ShutdownEventArgs>>();
        var shutdownLatch = new CountdownEvent(1);
        mockChannel1.SetupAdd(m => m.ModelShutdown += It.IsAny<EventHandler<RC.ShutdownEventArgs>>())
            .Callback<EventHandler<RC.ShutdownEventArgs>>(handler =>
            {
                listener.Value = handler;
                shutdownLatch.Signal();
            });
        var connectionFactory = new SingleConnectionFactory(mockConnectionFactory.Object);
        var template = new RabbitTemplate(connectionFactory)
        {
            ReplyTimeout = 60_000
        };
        var input = Message.Create(EncodingUtils.GetDefaultEncoding().GetBytes("Hello, world!"), new MessageHeaders());
        Task.Run(() =>
        {
            try
            {
                shutdownLatch.Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception)
            {
                // Ignore
            }

            listener.Value.Invoke(null, new RC.ShutdownEventArgs(RC.ShutdownInitiator.Peer, RabbitUtils.NotFound, string.Empty));
        });
        try
        {
            template.DoSendAndReceiveWithTemporary("foo", "bar", input, null, default);
            throw new Exception("Expected exception");
        }
        catch (RabbitException e)
        {
            var cause = e.InnerException;
            Assert.IsType<ShutdownSignalException>(cause);
        }
    }

    [Fact]
    public void TestAddAndRemoveBeforePublishPostProcessors()
    {
        var mpp1 = new DoNothingMessagePostProcessor();
        var mpp2 = new DoNothingMessagePostProcessor();
        var mpp3 = new DoNothingMessagePostProcessor();
        var rabbitTemplate = new RabbitTemplate();
        rabbitTemplate.AddBeforePublishPostProcessors(mpp1, mpp2);
        rabbitTemplate.AddBeforePublishPostProcessors(mpp3);
        var removed = rabbitTemplate.RemoveBeforePublishPostProcessor(mpp1);
        Assert.True(removed);
        Assert.Equal(2, rabbitTemplate.BeforePublishPostProcessors.Count);
        Assert.Contains(mpp2, rabbitTemplate.BeforePublishPostProcessors);
        Assert.Contains(mpp3, rabbitTemplate.BeforePublishPostProcessors);
    }

    [Fact]
    public void TestAddAndRemoveAfterReceivePostProcessors()
    {
        var mpp1 = new DoNothingMessagePostProcessor();
        var mpp2 = new DoNothingMessagePostProcessor();
        var mpp3 = new DoNothingMessagePostProcessor();
        var rabbitTemplate = new RabbitTemplate();
        rabbitTemplate.AddAfterReceivePostProcessors(mpp1, mpp2);
        rabbitTemplate.AddAfterReceivePostProcessors(mpp3);
        var removed = rabbitTemplate.RemoveAfterReceivePostProcessor(mpp1);
        Assert.True(removed);
        Assert.Equal(2, rabbitTemplate.AfterReceivePostProcessors.Count);
        Assert.Contains(mpp2, rabbitTemplate.AfterReceivePostProcessors);
        Assert.Contains(mpp3, rabbitTemplate.AfterReceivePostProcessors);
    }

    [Fact]
    public void TestPublisherConnWithInvoke()
    {
        var cf = new Mock<IConnectionFactory>();
        var pcf = new Mock<IConnectionFactory>();
        cf.SetupGet(c => c.PublisherConnectionFactory).Returns(pcf.Object);
        var template = new RabbitTemplate(cf.Object)
        {
            UsePublisherConnection = true
        };

        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<IPublisherCallbackChannel>();
        pcf.Setup(c => c.CreateConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateChannel(false)).Returns(mockChannel.Object);
        template.Invoke<object>(_ => null);
        pcf.Verify(c => c.CreateConnection());
        mockConnection.Verify(c => c.CreateChannel(false));
    }

    [Fact]
    public void TestPublisherConnWithInvokeInTx()
    {
        var cf = new Mock<IConnectionFactory>();
        var pcf = new Mock<IConnectionFactory>();
        cf.SetupGet(c => c.PublisherConnectionFactory).Returns(pcf.Object);
        var template = new RabbitTemplate(cf.Object)
        {
            UsePublisherConnection = true,
            IsChannelTransacted = true
        };

        var mockConnection = new Mock<IConnection>();
        var mockChannel = new Mock<RC.IModel>();

        pcf.Setup(c => c.CreateConnection()).Returns(mockConnection.Object);
        mockConnection.Setup(c => c.IsOpen).Returns(true);
        mockConnection.Setup(c => c.CreateChannel(true)).Returns(mockChannel.Object);
        template.Invoke<object>(_ => null);
        pcf.Verify(c => c.CreateConnection());
        mockConnection.Verify(c => c.CreateChannel(true));
    }

    private sealed class DoNothingMessagePostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message)
        {
            return message;
        }

        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return message;
        }
    }

    private sealed class TestRecoveryRecoveryCallback : IRecoveryCallback<object>
    {
        public TestRecoveryRecoveryCallback(AtomicBoolean boolean)
        {
            Boolean = boolean;
        }

        public AtomicBoolean Boolean { get; }

        public object Recover(IRetryContext context)
        {
            Boolean.Value = true;
            return null;
        }
    }

    private sealed class TestTransactionManager : AbstractPlatformTransactionManager
    {
        protected override object DoGetTransaction()
        {
            return new object();
        }

        protected override void DoBegin(object transaction, ITransactionDefinition definition)
        {
        }

        protected override void DoCommit(DefaultTransactionStatus status)
        {
        }

        protected override void DoRollback(DefaultTransactionStatus status)
        {
        }
    }
}
