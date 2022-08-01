// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Rabbit.Support;
using Steeltoe.Integration.Transformer;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Steeltoe.Messaging.Support;
using System.Text;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Integration.Rabbit.Inbound;

public class InboundEndpointTest
{
    [Fact]
    public void TestInt2809JavaTypePropertiesToRabbit()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new GenericApplicationContext(services, config);

        var channel = new Mock<RC.IModel>();
        channel.Setup(c => c.IsOpen).Returns(true);
        var connection = new Mock<Messaging.RabbitMQ.Connection.IConnection>();
        connection.Setup(c => c.IsOpen).Returns(true);
        connection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
        var connectionFactory = new Mock<Messaging.RabbitMQ.Connection.IConnectionFactory>();
        connectionFactory.Setup(f => f.CreateConnection()).Returns(connection.Object);
        var container = new DirectMessageListenerContainer();
        container.ConnectionFactory = connectionFactory.Object;
        container.AcknowledgeMode = Messaging.RabbitMQ.Core.AcknowledgeMode.Manual;
        var adapter = new RabbitInboundChannelAdapter(context, container) { MessageConverter = new JsonMessageConverter() };
        var queueChannel = new QueueChannel(context);
        adapter.OutputChannel = queueChannel;
        adapter.BindSourceMessage = true;
        object payload = new Foo("bar1");
        var objectToJsonTransformer = new ObjectToJsonTransformer(context, typeof(byte[]));
        var jsonMessage = objectToJsonTransformer.Transform(Message.Create(payload));

        var accessor = RabbitHeaderAccessor.GetMutableAccessor(jsonMessage);
        accessor.DeliveryTag = 123ul;
        var listener = container.MessageListener as IChannelAwareMessageListener;
        var rabbitChannel = new Mock<RC.IModel>();
        listener.OnMessage(jsonMessage, rabbitChannel.Object);
        var result = queueChannel.Receive(1000);
        Assert.Equal(payload, result.Payload);
        Assert.Same(rabbitChannel.Object, result.Headers.Get<RC.IModel>(RabbitMessageHeaders.Channel));
        Assert.Equal(123ul, result.Headers.DeliveryTag());
        var sourceData = result.Headers.Get<IMessage>(IntegrationMessageHeaderAccessor.SourceData);
        Assert.Same(jsonMessage, sourceData);
    }

    [Fact]
    public void TestInt2809JavaTypePropertiesFromAmqp()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new GenericApplicationContext(services, config);

        var channel = new Mock<RC.IModel>();
        channel.Setup(c => c.IsOpen).Returns(true);
        var connection = new Mock<Messaging.RabbitMQ.Connection.IConnection>();
        connection.Setup(c => c.IsOpen).Returns(true);
        connection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
        var connectionFactory = new Mock<Messaging.RabbitMQ.Connection.IConnectionFactory>();
        connectionFactory.Setup(f => f.CreateConnection()).Returns(connection.Object);
        var container = new DirectMessageListenerContainer();
        container.ConnectionFactory = connectionFactory.Object;
        var adapter = new RabbitInboundChannelAdapter(context, container);
        var queueChannel = new QueueChannel(context);
        adapter.OutputChannel = queueChannel;
        object payload = new Foo("bar1");
        var headers = new MessageHeaders();
        var amqpMessage = new JsonMessageConverter().ToMessage(payload, headers);
        var listener = container.MessageListener as IChannelAwareMessageListener;
        listener.OnMessage(amqpMessage, null);
        var receive = queueChannel.Receive(1000);
        var result = new JsonToObjectTransformer(context).Transform(receive);
        Assert.NotNull(result);
        Assert.Equal(payload, result.Payload);
        var sourceData = result.Headers.Get<IMessage>(IntegrationMessageHeaderAccessor.SourceData);
        Assert.Null(sourceData);
    }

    [Fact]
    public void TestAdapterConversionError()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new GenericApplicationContext(services, config);

        var channel = new Mock<RC.IModel>();
        channel.Setup(c => c.IsOpen).Returns(true);
        var connection = new Mock<Messaging.RabbitMQ.Connection.IConnection>();
        connection.Setup(c => c.IsOpen).Returns(true);
        connection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(channel.Object);
        var connectionFactory = new Mock<Messaging.RabbitMQ.Connection.IConnectionFactory>();
        connectionFactory.Setup(f => f.CreateConnection()).Returns(connection.Object);
        var container = new DirectMessageListenerContainer();
        container.ConnectionFactory = connectionFactory.Object;

        var adapter = new RabbitInboundChannelAdapter(context, container);
        var outputChannel = new QueueChannel(context);
        adapter.OutputChannel = outputChannel;
        var errorChannel = new QueueChannel(context);
        adapter.ErrorChannel = errorChannel;
        adapter.MessageConverter = new ThrowingMessageConverter();

        var accessor = new RabbitHeaderAccessor { DeliveryTag = 123ul };
        var headers = accessor.MessageHeaders;
        var message = Message.Create(string.Empty, headers);
        var listener = container.MessageListener as IChannelAwareMessageListener;
        listener.OnMessage(message, null);
        Assert.Null(outputChannel.Receive(0));
        var received = errorChannel.Receive(0);
        Assert.NotNull(received.Headers.Get<IMessage>(RabbitMessageHeaderErrorMessageStrategy.AmqpRawMessage));
        Assert.IsType<ListenerExecutionFailedException>(received.Payload);

        container.AcknowledgeMode = Messaging.RabbitMQ.Core.AcknowledgeMode.Manual;
        var channel2 = new Mock<RC.IModel>();
        listener.OnMessage(message, channel2.Object);
        Assert.Null(outputChannel.Receive(0));
        received = errorChannel.Receive(0);
        Assert.NotNull(received.Headers.Get<IMessage>(RabbitMessageHeaderErrorMessageStrategy.AmqpRawMessage));
        Assert.IsType<ManualAckListenerExecutionFailedException>(received.Payload);
        var ex = (ManualAckListenerExecutionFailedException)received.Payload;
        Assert.Same(channel2.Object, ex.Channel);
        Assert.Equal(123ul, ex.DeliveryTag);
    }

    [Fact]
    public void TestRetryWithinOnMessageAdapter()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new GenericApplicationContext(services, config);

        var container = new DirectMessageListenerContainer();
        var adapter = new RabbitInboundChannelAdapter(context, container)
        {
            OutputChannel = new DirectChannel(context), RetryTemplate = new PollyRetryTemplate(3, 1, 1, 1)
        };

        var errors = new QueueChannel(context);
        var recoveryCallback = new ErrorMessageSendingRecoverer(context, errors) { ErrorMessageStrategy = new RabbitMessageHeaderErrorMessageStrategy() };
        adapter.RecoveryCallback = recoveryCallback;
        var listener = container.MessageListener as IChannelAwareMessageListener;
        var message = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("foo")).CopyHeaders(new MessageHeaders()).Build();
        listener.OnMessage(message, null);
        var errorMessage = errors.Receive(0);
        Assert.NotNull(errorMessage);
        var payload = errorMessage.Payload as MessagingException;
        Assert.NotNull(payload);
        Assert.Contains("Dispatcher has no", payload.Message);
        var deliveryAttempts = payload.FailedMessage.Headers.Get<AtomicInteger>(IntegrationMessageHeaderAccessor.DeliveryAttempt);
        Assert.NotNull(deliveryAttempts);
        Assert.Equal(3, deliveryAttempts.Value);
        var amqpMessage = errorMessage.Headers.Get<IMessage>(RabbitMessageHeaderErrorMessageStrategy.AmqpRawMessage);
        Assert.NotNull(amqpMessage);
        Assert.Null(errors.Receive(0));
    }

    [Fact]
    public void TestBatchedAdapter()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = new GenericApplicationContext(services, config);

        var container = new DirectMessageListenerContainer();
        var adapter = new RabbitInboundChannelAdapter(context, container);
        var outChannel = new QueueChannel(context);
        adapter.OutputChannel = outChannel;
        var listener = container.MessageListener as IChannelAwareMessageListener;
        var bs = new SimpleBatchingStrategy(2, 10_000, 10_000L);
        var accessor = new MessageHeaderAccessor { ContentType = "text/plain" };
        var message = Message.Create(Encoding.UTF8.GetBytes("test1"), accessor.MessageHeaders);
        bs.AddToBatch("foo", "bar", message);
        message = Message.Create(Encoding.UTF8.GetBytes("test2"), accessor.MessageHeaders);
        var batched = bs.AddToBatch("foo", "bar", message);
        Assert.NotNull(batched);
        listener.OnMessage(batched.Value.Message, null);
        var received = outChannel.Receive();
        Assert.NotNull(received);
        var asList = received.Payload as List<object>;
        Assert.NotNull(asList);
        Assert.Equal(2, asList.Count);
        Assert.Contains("test1", asList);
        Assert.Contains("test2", asList);
    }

    public class ThrowingMessageConverter : ISmartMessageConverter
    {
        public string ServiceName { get; set; }

        public object FromMessage(IMessage message, Type targetClass)
        {
            throw new MessageConversionException("intended");
        }

        public T FromMessage<T>(IMessage message)
        {
            throw new MessageConversionException("intended");
        }

        public object FromMessage(IMessage message, Type targetClass, object conversionHint)
        {
            throw new MessageConversionException("intended");
        }

        public T FromMessage<T>(IMessage message, object conversionHint)
        {
            throw new MessageConversionException("intended");
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers)
        {
            throw new MessageConversionException("intended");
        }

        public IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint)
        {
            throw new MessageConversionException("intended");
        }
    }

    // public class TestMessageTransformer : ITransformer
    // {
    //    public TestMessageTransformer(RC.IModel channel, ulong deliveryTag)
    //    {
    //        Channel = channel;
    //        DeliveryTag = deliveryTag;
    //    }

    // public IModel Channel { get; }

    // public ulong DeliveryTag { get; }

    // public IMessage Transform(IMessage message)
    //    {
    //        var headers = message.Headers;
    //        Assert.Equal(Channel, headers.Get<RC.IModel>(RabbitMessageHeaders.CHANNEL));
    //        Assert.Equal(DeliveryTag, headers.DeliveryTag());
    //        return IntegrationMessageBuilder.FromMessage(message)
    //            .SetHeader(MessageHeaders.TYPE_ID, "foo")
    //            .SetHeader(MessageHeaders.CONTENT_TYPE_ID, "bar")
    //            .SetHeader(MessageHeaders.KEY_TYPE_ID, "baz")
    //            .Build();
    //    }
    // }
    public class Foo
    {
        public string Bar { get; }

        public Foo(string bar)
        {
            Bar = bar;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not Foo other)
            {
                return false;
            }

            return Bar == other.Bar;
        }

        public override int GetHashCode()
        {
            return Bar?.GetHashCode() ?? 0;
        }
    }
}
