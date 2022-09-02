// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binder.RabbitMQ.Configuration;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Configuration;
using Steeltoe.Stream.Converter;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Stream.Binder;

public abstract class AbstractBinderTests<TTestBinder, TBinder>
    where TTestBinder : AbstractTestBinder<TBinder>
    where TBinder : AbstractBinder<IMessageChannel>
{
    protected virtual ISmartMessageConverter MessageConverter { get; set; }

    protected virtual double TimeoutMultiplier { get; set; } = 1.0D;

    protected virtual ITestOutputHelper Output { get; set; }

    protected virtual ServiceCollection Services { get; set; }

    protected virtual ConfigurationBuilder ConfigurationBuilder { get; set; }

    protected CachingConnectionFactory CachingConnectionFactory { get; set; }

    public ILoggerFactory LoggerFactory { get; }

    protected AbstractBinderTests(ITestOutputHelper output, ILoggerFactory loggerFactory)
    {
        MessageConverter = new CompositeMessageConverterFactory().MessageConverterForAllRegistered;
        LoggerFactory = loggerFactory;
        Output = output;
        Services = new ServiceCollection();
        ConfigurationBuilder = new ConfigurationBuilder();
    }

    [Fact]
    public void TestClean()
    {
        var bindingsOptions = new RabbitBindingsOptions();
        TTestBinder binder = GetBinder();
        string delimiter = GetDestinationNameDelimiter();

        IBinding foo0ProducerBinding = binder.BindProducer($"foo{delimiter}0", CreateBindableChannel("output", GetDefaultBindingOptions()),
            GetProducerOptions("output", bindingsOptions));

        IBinding foo0ConsumerBinding = binder.BindConsumer($"foo{delimiter}0", "testClean", CreateBindableChannel("input", GetDefaultBindingOptions()),
            GetConsumerOptions("input", bindingsOptions));

        IBinding foo1ProducerBinding = binder.BindProducer($"foo{delimiter}1", CreateBindableChannel("output", GetDefaultBindingOptions()),
            GetProducerOptions("output1", bindingsOptions));

        IBinding foo1ConsumerBinding = binder.BindConsumer($"foo{delimiter}1", "testClean", CreateBindableChannel("input", GetDefaultBindingOptions()),
            GetConsumerOptions("input1", bindingsOptions));

        IBinding foo2ProducerBinding = binder.BindProducer($"foo{delimiter}2", CreateBindableChannel("output", GetDefaultBindingOptions()),
            GetProducerOptions("output2", bindingsOptions));

        foo0ProducerBinding.UnbindAsync();
        Assert.False(foo0ProducerBinding.IsRunning);

        foo0ConsumerBinding.UnbindAsync();
        foo1ProducerBinding.UnbindAsync();

        Assert.False(foo0ConsumerBinding.IsRunning);
        Assert.False(foo1ProducerBinding.IsRunning);

        foo1ConsumerBinding.UnbindAsync();
        foo2ProducerBinding.UnbindAsync();

        Assert.False(foo1ConsumerBinding.IsRunning);
        Assert.False(foo2ProducerBinding.IsRunning);
    }

    [Fact]
    public void TestSendAndReceive()
    {
        var bindingsOptions = new RabbitBindingsOptions();
        TTestBinder binder = GetBinder(bindingsOptions);
        string delimiter = GetDestinationNameDelimiter();

        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        DirectChannel moduleOutputChannel = CreateBindableChannel("output", GetDefaultBindingOptions());

        DirectChannel moduleInputChannel = CreateBindableChannel("input", GetDefaultBindingOptions());

        IBinding producerBinding = binder.BindProducer($"foo{delimiter}0", moduleOutputChannel, producerOptions);
        IBinding consumerBinding = binder.BindConsumer($"foo{delimiter}0", "testSendAndReceive", moduleInputChannel, consumerProperties);
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader(MessageHeaders.ContentType, MimeType.ToMimeType("text/plain")).Build();

        var latch = new CountdownEvent(1);
        var inboundMessageRef = new AtomicReference<IMessage>();

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = message =>
            {
                inboundMessageRef.GetAndSet(message);
                latch.Signal();
            }
        });

        moduleOutputChannel.Send(message);

        Assert.True(latch.Wait(TimeSpan.FromSeconds(5)), "Failed to receive message");

        IMessage actual = inboundMessageRef.Value;
        Assert.Equal("foo".GetBytes(), actual.Payload);
        Assert.Equal("text/plain", actual.Headers.ContentType());
        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestSendAndReceiveMultipleTopics()
    {
        TTestBinder binder = GetBinder();

        string delimiter = GetDestinationNameDelimiter();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerOptions);

        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        DirectChannel moduleOutputChannel1 = CreateBindableChannel("output1", producerBindingProperties);

        DirectChannel moduleOutputChannel2 = CreateBindableChannel("output2", producerBindingProperties);
        var moduleInputChannel = new QueueChannel();
        IBinding producerBinding1 = binder.BindProducer($"foo{delimiter}xy", moduleOutputChannel1, producerBindingProperties.Producer);
        IBinding producerBinding2 = binder.BindProducer($"foo{delimiter}yz", moduleOutputChannel2, producerBindingProperties.Producer);
        IBinding consumerBinding1 = binder.BindConsumer($"foo{delimiter}xy", "testSendAndReceiveMultipleTopics", moduleInputChannel, consumerProperties);
        IBinding consumerBinding2 = binder.BindConsumer($"foo{delimiter}yz", "testSendAndReceiveMultipleTopics", moduleInputChannel, consumerProperties);

        string testPayload1 = $"foo{Guid.NewGuid()}";
        IMessage message1 = MessageBuilder.WithPayload(testPayload1.GetBytes()).SetHeader("contentType", MimeTypeUtils.ApplicationOctetStream).Build();
        string testPayload2 = $"foo{Guid.NewGuid()}";
        IMessage message2 = MessageBuilder.WithPayload(testPayload2.GetBytes()).SetHeader("contentType", MimeTypeUtils.ApplicationOctetStream).Build();

        BinderBindUnbindLatency();
        moduleOutputChannel1.Send(message1);
        moduleOutputChannel2.Send(message2);

        IMessage[] messages =
        {
            Receive(moduleInputChannel),
            Receive(moduleInputChannel)
        };

        Assert.NotNull(messages[0]);
        Assert.NotNull(messages[1]);

        Assert.Contains(messages, m => ((byte[])m.Payload).GetString() == testPayload1);
        Assert.Contains(messages, m => ((byte[])m.Payload).GetString() == testPayload2);

        producerBinding1.UnbindAsync();
        producerBinding2.UnbindAsync();
        consumerBinding1.UnbindAsync();
        consumerBinding2.UnbindAsync();
    }

    [Fact]
    public void TestSendAndReceiveNoOriginalContentType()
    {
        TTestBinder binder = GetBinder();

        string delimiter = GetDestinationNameDelimiter();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerOptions);

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", producerBindingProperties);

        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        BindingOptions inputBindingProperties = CreateConsumerBindingOptions(consumerProperties);
        DirectChannel moduleInputChannel = CreateBindableChannel("input", inputBindingProperties);

        IBinding producerBinding = binder.BindProducer($"bar{delimiter}0", moduleOutputChannel, producerBindingProperties.Producer);
        IBinding consumerBinding = binder.BindConsumer($"bar{delimiter}0", "testSendAndReceiveNoOriginalContentType", moduleInputChannel, consumerProperties);
        BinderBindUnbindLatency();
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("contentType", MimeTypeUtils.TextPlain).Build();

        moduleOutputChannel.Send(message);
        var latch = new CountdownEvent(1);
        var inboundMessageRef = new AtomicReference<IMessage>();

        moduleInputChannel.Subscribe(new TestMessageHandler
        {
            OnHandleMessage = message =>
            {
                inboundMessageRef.GetAndSet(message);
                latch.Signal();
            }
        });

        moduleOutputChannel.Send(message);

        Assert.True(latch.Wait(TimeSpan.FromSeconds(5)), "Failed to receive message");
        Assert.NotNull(inboundMessageRef.Value);
        Assert.Equal("foo", ((byte[])inboundMessageRef.Value.Payload).GetString());
        Assert.Equal("text/plain", inboundMessageRef.Value.Headers.ContentType());

        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestSendPojoReceivePojoWithStreamListenerDefaultContentType()
    {
        StreamListenerMessageHandler handler = BuildStreamListener(GetType(), "EchoStation", typeof(Station));
        TTestBinder binder = GetBinder();

        string delimiter = GetDestinationNameDelimiter();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerOptions);

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", producerBindingProperties);
        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        BindingOptions consumerBindingProperties = CreateConsumerBindingOptions(consumerProperties);

        DirectChannel moduleInputChannel = CreateBindableChannel("input", consumerBindingProperties);
        IBinding producerBinding = binder.BindProducer($"bar{delimiter}0a", moduleOutputChannel, producerBindingProperties.Producer);
        IBinding consumerBinding = binder.BindConsumer($"bar{delimiter}0a", "test-1", moduleInputChannel, consumerBindingProperties.Consumer);

        var station = new Station();
        IMessage message = MessageBuilder.WithPayload(station).Build();
        moduleInputChannel.Subscribe(handler);
        moduleOutputChannel.Send(message);
        var replyChannel = (QueueChannel)handler.OutputChannel;
        IMessage replyMessage = replyChannel.Receive(5000);

        Assert.IsType<Station>(replyMessage.Payload);
        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestSendJsonReceivePojoWithStreamListener()
    {
        StreamListenerMessageHandler handler = BuildStreamListener(GetType(), "EchoStation", typeof(Station));
        TTestBinder binder = GetBinder();

        string delimiter = GetDestinationNameDelimiter();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerOptions);

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", producerBindingProperties);
        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        BindingOptions consumerBindingProperties = CreateConsumerBindingOptions(consumerProperties);

        DirectChannel moduleInputChannel = CreateBindableChannel("input", consumerBindingProperties);
        IBinding producerBinding = binder.BindProducer($"bad{delimiter}0d", moduleOutputChannel, producerBindingProperties.Producer);
        IBinding consumerBinding = binder.BindConsumer($"bad{delimiter}0d", "test-4", moduleInputChannel, consumerBindingProperties.Consumer);

        const string value =
            "{\"readings\":[{\"stationid\":\"fgh\",\"customerid\":\"12345\",\"timestamp\":null},{\"stationid\":\"hjk\",\"customerid\":\"222\",\"timestamp\":null}]}";

        IMessage message = MessageBuilder.WithPayload(value).SetHeader("contentType", MimeTypeUtils.ApplicationJson).Build();
        moduleInputChannel.Subscribe(handler);
        moduleOutputChannel.Send(message);
        var channel = (QueueChannel)handler.OutputChannel;
        IMessage reply = channel.Receive(5000);

        Assert.NotNull(reply);
        Assert.IsType<Station>(reply.Payload);
        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestSendJsonReceiveJsonWithStreamListener()
    {
        StreamListenerMessageHandler handler = BuildStreamListener(GetType(), "EchoStationString", typeof(string));
        TTestBinder binder = GetBinder();

        string delimiter = GetDestinationNameDelimiter();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerOptions);

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", producerBindingProperties);
        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        BindingOptions consumerBindingProperties = CreateConsumerBindingOptions(consumerProperties);

        DirectChannel moduleInputChannel = CreateBindableChannel("input", consumerBindingProperties);
        IBinding producerBinding = binder.BindProducer($"bad{delimiter}0e", moduleOutputChannel, producerBindingProperties.Producer);
        IBinding consumerBinding = binder.BindConsumer($"bad{delimiter}0e", "test-5", moduleInputChannel, consumerBindingProperties.Consumer);

        const string value =
            "{\"readings\":[{\"stationid\":\"fgh\",\"customerid\":\"12345\",\"timestamp\":null},{\"stationid\":\"hjk\",\"customerid\":\"222\",\"timestamp\":null}]}";

        IMessage message = MessageBuilder.WithPayload(value).SetHeader("contentType", MimeTypeUtils.ApplicationJson).Build();

        moduleInputChannel.Subscribe(handler);
        moduleOutputChannel.Send(message);
        var channel = (QueueChannel)handler.OutputChannel;
        IMessage reply = channel.Receive(5000);
        Assert.NotNull(reply);
        Assert.IsType<string>(reply.Payload);
        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    [Fact]
    public void TestSendPojoReceivePojoWithStreamListener()
    {
        StreamListenerMessageHandler handler = BuildStreamListener(GetType(), "EchoStation", typeof(Station));
        TTestBinder binder = GetBinder();

        string delimiter = GetDestinationNameDelimiter();
        var bindingsOptions = new RabbitBindingsOptions();
        ProducerOptions producerOptions = GetProducerOptions("input", bindingsOptions);
        BindingOptions producerBindingProperties = CreateProducerBindingOptions(producerOptions);

        DirectChannel moduleOutputChannel = CreateBindableChannel("output", producerBindingProperties);
        ConsumerOptions consumerProperties = GetConsumerOptions("output", bindingsOptions);
        BindingOptions consumerBindingProperties = CreateConsumerBindingOptions(consumerProperties);

        DirectChannel moduleInputChannel = CreateBindableChannel("input", consumerBindingProperties);
        IBinding producerBinding = binder.BindProducer($"bad{delimiter}0f", moduleOutputChannel, producerBindingProperties.Producer);
        IBinding consumerBinding = binder.BindConsumer($"bad{delimiter}0f", "test-6", moduleInputChannel, consumerBindingProperties.Consumer);

        var r1 = new Station.Readings
        {
            CustomerId = "123",
            StationId = "XYZ"
        };

        var r2 = new Station.Readings
        {
            CustomerId = "546",
            StationId = "ABC"
        };

        var station = new Station
        {
            ReadingsList = new List<Station.Readings>
            {
                r1,
                r2
            }
        };

        IMessage message = MessageBuilder.WithPayload(station).SetHeader("contentType", MimeTypeUtils.ApplicationJson).Build();
        moduleInputChannel.Subscribe(handler);
        moduleOutputChannel.Send(message);
        var channel = (QueueChannel)handler.OutputChannel;
        IMessage reply = channel.Receive(5000);
        Assert.NotNull(reply);
        Assert.IsType<Station>(reply.Payload);
        producerBinding.UnbindAsync();
        consumerBinding.UnbindAsync();
    }

    protected CachingConnectionFactory GetResource()
    {
        if (CachingConnectionFactory == null)
        {
            CachingConnectionFactory = new CachingConnectionFactory("localhost");
            CachingConnectionFactory.CreateConnection().Close();
        }

        return CachingConnectionFactory;
    }

    protected BindingOptions CreateConsumerBindingOptions(ConsumerOptions consumerOptions)
    {
        return new BindingOptions
        {
            ContentType = BindingOptions.DefaultContentType.ToString(),
            Consumer = consumerOptions
        };
    }

    protected BindingOptions CreateProducerBindingOptions(ProducerOptions producerOptions)
    {
        return new BindingOptions
        {
            ContentType = BindingOptions.DefaultContentType.ToString(),
            Producer = producerOptions
        };
    }

    protected IMessage Receive(IPollableChannel channel)
    {
        return Receive(channel, 1);
    }

    protected IMessage Receive(IPollableChannel channel, int additionalMultiplier)
    {
        return channel.Receive((int)(1000 * TimeoutMultiplier * additionalMultiplier));
    }

    protected DirectChannel CreateBindableChannel(string channelName, BindingOptions bindingProperties)
    {
        // The 'channelName.contains("input")' is strictly for convenience to avoid
        // modifications in multiple tests
        return CreateBindableChannel(channelName, bindingProperties, channelName.Contains("input"));
    }

    protected DirectChannel CreateBindableChannel(string channelName, BindingOptions bindingProperties, bool inputChannel)
    {
        MessageConverterConfigurer messageConverterConfigurer = CreateConverterConfigurer(channelName, bindingProperties);

        var channel = new DirectChannel(LoggerFactory.CreateLogger<DirectChannel>())
        {
            ServiceName = channelName
        };

        if (inputChannel)
        {
            messageConverterConfigurer.ConfigureInputChannel(channel, channelName);
        }
        else
        {
            messageConverterConfigurer.ConfigureOutputChannel(channel, channelName);
        }

        return channel;
    }

    protected string GetDestinationNameDelimiter()
    {
        return ".";
    }

    protected TTestBinder GetBinder()
    {
        return GetBinder(null);
    }

    protected abstract TTestBinder GetBinder(RabbitBindingsOptions bindingsOptions);

    protected void BinderBindUnbindLatency()
    {
    }

    protected ConsumerOptions GetConsumerOptions(string bindingName, RabbitBindingsOptions bindingsOptions)
    {
        return GetConsumerOptions(bindingName, bindingsOptions, null, null);
    }

    protected ConsumerOptions GetConsumerOptions(string bindingName, RabbitBindingsOptions bindingsOptions, RabbitConsumerOptions rabbitConsumerOptions,
        RabbitBindingOptions bindingOptions)
    {
        rabbitConsumerOptions ??= new RabbitConsumerOptions();
        rabbitConsumerOptions.PostProcess();

        bindingOptions ??= new RabbitBindingOptions();
        bindingOptions.Consumer = rabbitConsumerOptions;
        bindingsOptions.Bindings.Add(bindingName, bindingOptions);

        var consumerOptions = new ConsumerOptions
        {
            BindingName = bindingName
        };

        consumerOptions.PostProcess(bindingName);
        return consumerOptions;
    }

    protected ProducerOptions GetProducerOptions(string bindingName, RabbitBindingsOptions bindingsOptions)
    {
        return GetProducerOptions(bindingName, bindingsOptions, null);
    }

    protected ProducerOptions GetProducerOptions(string bindingName, RabbitBindingsOptions bindingsOptions, RabbitBindingOptions bindingOptions)
    {
        var rabbitProducerOptions = new RabbitProducerOptions();
        rabbitProducerOptions.PostProcess();

        bindingOptions ??= new RabbitBindingOptions();

        bindingOptions.Producer = rabbitProducerOptions;

        bindingsOptions.Bindings.Add(bindingName, bindingOptions);

        var producerOptions = new ProducerOptions
        {
            BindingName = bindingName
        };

        producerOptions.PostProcess(bindingName);

        return producerOptions;
    }

    protected BindingOptions GetDefaultBindingOptions()
    {
        return new BindingOptions
        {
            ContentType = BindingOptions.DefaultContentType.ToString()
        };
    }

    public Station EchoStation(Station station)
    {
        return station;
    }

    public string EchoStationString(string station)
    {
        return station;
    }

    private StreamListenerMessageHandler BuildStreamListener(Type handlerType, string handlerMethodName, params Type[] parameters)
    {
        string channelName = $"reply_{default(DateTime).Ticks}";
        TTestBinder binder = GetBinder();

        binder.ApplicationContext.Register(channelName, new QueueChannel());

        MethodInfo methodInfo = handlerType.GetMethod(handlerMethodName, parameters);
        var method = new InvocableHandlerMethod(this, methodInfo);
        var resolver = new HandlerMethodArgumentResolverComposite();
        var factory = new CompositeMessageConverterFactory();

        resolver.AddResolver(new PayloadArgumentResolver(factory.MessageConverterForAllRegistered));
        method.MessageMethodArgumentResolvers = resolver;

        ConstructorInfo constructor = typeof(StreamListenerMessageHandler).GetConstructor(new[]
        {
            typeof(IApplicationContext),
            typeof(InvocableHandlerMethod),
            typeof(bool),
            typeof(string[])
        });

        var handler = (StreamListenerMessageHandler)constructor.Invoke(new object[]
        {
            binder.ApplicationContext,
            method,
            false,
            Array.Empty<string>()
        });

        handler.OutputChannelName = channelName;
        return handler;
    }

    private MessageConverterConfigurer CreateConverterConfigurer(string channelName, BindingOptions bindingProperties)
    {
        var bindingServiceProperties = new BindingServiceOptions();
        bindingServiceProperties.Bindings.Add(channelName, bindingProperties);
        IApplicationContext applicationContext = GetBinder().ApplicationContext;

        IEnumerable<IPartitionKeyExtractorStrategy> extractors = applicationContext.GetServices<IPartitionKeyExtractorStrategy>();
        IEnumerable<IPartitionSelectorStrategy> selectors = applicationContext.GetServices<IPartitionSelectorStrategy>();
        var bindingServiceOptionsMonitor = new BindingServiceOptionsMonitor(bindingServiceProperties);

        var messageConverterConfigurer = new MessageConverterConfigurer(applicationContext, bindingServiceOptionsMonitor,
            new CompositeMessageConverterFactory(), extractors, selectors);

        return messageConverterConfigurer;
    }

    protected class TestMessageHandler : IMessageHandler
    {
        public Action<IMessage> OnHandleMessage { get; set; }

        public string ServiceName
        {
            get => "TestMessageHandler";
            set => throw new NotImplementedException();
        }

        public void HandleMessage(IMessage message)
        {
            OnHandleMessage.Invoke(message);
        }
    }

    public class Station
    {
        public List<Readings> ReadingsList { get; set; }

        public class Readings
        {
            public string StationId { get; set; }

            public string CustomerId { get; set; }

            public string TimeStamp { get; set; }
        }
    }

    private sealed class BindingServiceOptionsMonitor : IOptionsMonitor<BindingServiceOptions>
    {
        public BindingServiceOptions CurrentValue { get; }

        public BindingServiceOptionsMonitor(BindingServiceOptions options)
        {
            CurrentValue = options;
        }

        public BindingServiceOptions Get(string name)
        {
            return CurrentValue;
        }

        public IDisposable OnChange(Action<BindingServiceOptions, string> listener)
        {
            return null;
        }
    }
}
