// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration;
using Steeltoe.Integration.Acks;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class PollableConsumerTest : AbstractTest
{
    [Fact]
    public void TestSimple()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");
        pollableSource.AddInterceptor(new TestSimpleChannelInterceptor());

        var properties = new ConsumerOptions
        {
            MaxAttempts = 2,
            BackOffInitialInterval = 0
        };
        properties.PostProcess("testbinding");

        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var handler = new TestSimpleHandler();
        Assert.True(pollableSource.Poll(handler));

        Assert.Equal(2, handler.Count);
    }

    [Fact]
    public void TestConvertSimple()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var setter = binder.GetType().GetProperty("MessageSourceDelegate").GetSetMethod();
        var messageSource = new TestMessageSource("{\"foo\":\"bar\"}");
        setter.Invoke(binder, new object[] { messageSource });

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");

        var properties = new ConsumerOptions
        {
            MaxAttempts = 2,
            BackOffInitialInterval = 0
        };
        properties.PostProcess("testbinding");

        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var handler = new TestConvertSimpleHandler();
        Assert.True(pollableSource.Poll(handler, typeof(FooType)));

        Assert.IsType<FooType>(handler.Payload);
        var fooType = handler.Payload as FooType;
        Assert.Equal("bar", fooType.Foo);

        Assert.True(pollableSource.Poll(handler, typeof(FooType)));

        Assert.IsType<FooType>(handler.Payload);
        fooType = handler.Payload as FooType;
        Assert.Equal("bar", fooType.Foo);
    }

    [Fact]
    public void TestConvertSimpler()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories, "spring:cloud:stream:bindings:foo:contentType=text/plain").BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var options = serviceProvider.GetService<IOptions<BindingServiceOptions>>();
        options.Value.Bindings.TryGetValue("foo", out var bindingOptions);
        Assert.Equal("text/plain", bindingOptions.ContentType);

        var setter = binder.GetType().GetProperty("MessageSourceDelegate").GetSetMethod();
        var messageSource = new TestMessageSource("foo");
        setter.Invoke(binder, new object[] { messageSource });

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");
        var properties = new ConsumerOptions
        {
            MaxAttempts = 1,
            BackOffInitialInterval = 0
        };
        properties.PostProcess("testbinding");

        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var handler = new TestConvertSimpleHandler();
        Assert.True(pollableSource.Poll(handler, typeof(string)));

        Assert.IsType<string>(handler.Payload);
        var str = handler.Payload as string;
        Assert.Equal("foo", str);

        Assert.True(pollableSource.Poll(handler, typeof(string)));

        Assert.IsType<string>(handler.Payload);
        str = handler.Payload as string;
        Assert.Equal("foo", str);
    }

    [Fact]
    public void TestConvertList()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var setter = binder.GetType().GetProperty("MessageSourceDelegate").GetSetMethod();
        var messageSource = new TestMessageSource("[{\"foo\":\"bar\"},{\"foo\":\"baz\"}]");
        setter.Invoke(binder, new object[] { messageSource });

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");

        var properties = new ConsumerOptions
        {
            MaxAttempts = 1,
            BackOffInitialInterval = 0
        };
        properties.PostProcess("testbinding");

        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var handler = new TestConvertSimpleHandler();
        Assert.True(pollableSource.Poll(handler, typeof(List<FooType>)));

        Assert.IsType<List<FooType>>(handler.Payload);
        var list = handler.Payload as List<FooType>;
        Assert.Equal("bar", list[0].Foo);
        Assert.Equal("baz", list[1].Foo);
    }

    [Fact]
    public void TestConvertMap()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var setter = binder.GetType().GetProperty("MessageSourceDelegate").GetSetMethod();
        var messageSource = new TestMessageSource("{\"qux\":{\"foo\":\"bar\"}}");
        setter.Invoke(binder, new object[] { messageSource });

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");

        var properties = new ConsumerOptions
        {
            MaxAttempts = 1,
            BackOffInitialInterval = 0
        };
        properties.PostProcess("testbinding");

        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var handler = new TestConvertSimpleHandler();
        Assert.True(pollableSource.Poll(handler, typeof(Dictionary<string, FooType>)));

        Assert.IsType<Dictionary<string, FooType>>(handler.Payload);
        var dict = handler.Payload as Dictionary<string, FooType>;
        Assert.Single(dict);
        Assert.Equal("bar", dict["qux"].Foo);
    }

    [Fact]
    public void TestEmbedded()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(
            searchDirectories,
            $"spring:cloud:stream:bindings:foo:consumer:headerMode={HeaderMode.EmbeddedHeaders}").BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var setter = binder.GetType().GetProperty("MessageSourceDelegate").GetSetMethod();
        var messageSource = new TestFuncMessageSource(() =>
        {
            var original = new MessageValues(
                Encoding.UTF8.GetBytes("foo"),
                new Dictionary<string, object> { { MessageHeaders.CONTENT_TYPE, "application/octet-stream" } });
            var payload = Array.Empty<byte>();
            try
            {
                payload = EmbeddedHeaderUtils.EmbedHeaders(original, MessageHeaders.CONTENT_TYPE);
            }
            catch (Exception e)
            {
                Assert.NotNull(e);
            }

            return Message.Create(payload);
        });

        setter.Invoke(binder, new object[] { messageSource });

        var options = serviceProvider.GetService<IOptions<BindingServiceOptions>>();
        options.Value.Bindings.TryGetValue("foo", out var bindingOptions);
        Assert.Equal(HeaderMode.EmbeddedHeaders, bindingOptions.Consumer.HeaderMode);

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");
        pollableSource.AddInterceptor(new TestEmbededChannelInterceptor());

        binder.BindConsumer("foo", "bar", pollableSource, bindingOptions.Consumer);

        var handler = new TestConvertSimpleHandler();
        Assert.True(pollableSource.Poll(handler));

        Assert.IsType<string>(handler.Payload);
        var str = handler.Payload as string;
        Assert.Equal("FOO", str);
        handler.Message.Headers.TryGetValue(MessageHeaders.CONTENT_TYPE, out var contentType);
        Assert.Equal("application/octet-stream", contentType.ToString());
    }

    [Fact]
    public void TestErrors()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");
        pollableSource.AddInterceptor(new TestSimpleChannelInterceptor());

        var properties = new ConsumerOptions
        {
            MaxAttempts = 2,
            BackOffInitialInterval = 0,
            RetryableExceptions = new List<string> { "!System.InvalidOperationException" }
        };
        properties.PostProcess("testbinding");

        var latch = new CountdownEvent(2);
        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var errorChan = serviceProvider.GetServices<IMessageChannel>().Single(chan => chan.ServiceName == IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME) as ISubscribableChannel;
        var errorChanHandler = new TestErrorsErrorChannelHandler(latch);
        errorChan.Subscribe(errorChanHandler);

        var h1 = new TestFuncMessageHandler(m => throw new Exception("test recoverer"));

        Assert.True(pollableSource.Poll(h1));
        Assert.Equal(2, h1.Count);

        var getter = binder.GetType().GetProperty("LastError").GetGetMethod();

        var lastError = getter.Invoke(binder, Array.Empty<object>()) as IMessage;
        Assert.NotNull(lastError);

        var lastErrorMessage = ((Exception)lastError.Payload).InnerException.Message;
        Assert.Equal("test recoverer", lastErrorMessage);

        var h2 = new TestFuncMessageHandler(m => throw new InvalidOperationException("no retries"));

        Assert.True(pollableSource.Poll(h2));
        Assert.Equal(1, h2.Count);

        lastError = getter.Invoke(binder, Array.Empty<object>()) as IMessage;
        lastErrorMessage = ((Exception)lastError.Payload).InnerException.Message;
        Assert.Equal("no retries", lastErrorMessage);
    }

    [Fact]
    public void TestErrorsNoRetry()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");
        pollableSource.AddInterceptor(new TestSimpleChannelInterceptor());

        var properties = new ConsumerOptions
        {
            MaxAttempts = 1
        };
        properties.PostProcess("testbinding");

        var latch = new CountdownEvent(1);
        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var errorChan = serviceProvider.GetServices<IMessageChannel>().Single(chan => chan.ServiceName == IntegrationContextUtils.ERROR_CHANNEL_BEAN_NAME) as ISubscribableChannel;
        var errorChanHandler = new TestErrorsErrorChannelHandler(latch);
        errorChan.Subscribe(errorChanHandler);

        var h1 = new TestFuncMessageHandler(m => throw new Exception("test recoverer"));

        Assert.True(pollableSource.Poll(h1));
        Assert.Equal(1, h1.Count);
    }

    [Fact]
    public void TestRequeue()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        IServiceProvider serviceProvider = CreateStreamsContainer(searchDirectories).BuildServiceProvider();
        var messageConverter = serviceProvider.GetService<ISmartMessageConverter>();
        Assert.NotNull(messageConverter);

        var binder = serviceProvider.GetService<IBinder>() as AbstractPollableMessageSourceBinder;
        Assert.NotNull(binder);

        var configurer = serviceProvider.GetService<MessageConverterConfigurer>();
        Assert.NotNull(configurer);

        var pollableSource = new DefaultPollableMessageSource(serviceProvider.GetService<IApplicationContext>(), messageConverter);
        configurer.ConfigurePolledMessageSource(pollableSource, "foo");

        var mockCallback = new Mock<IAcknowledgmentCallback>(MockBehavior.Default);

        pollableSource.AddInterceptor(new TestRequeueChannelInterceptor(mockCallback));
        var properties = new ConsumerOptions
        {
            MaxAttempts = 2,
            BackOffInitialInterval = 0
        };
        properties.PostProcess("testbinding");

        binder.BindConsumer("foo", "bar", pollableSource, properties);
        var h1 = new TestFuncMessageHandler(m => throw new RequeueCurrentMessageException("test retry"));

        Assert.True(pollableSource.Poll(h1));
        Assert.Equal(2, h1.Count);
        mockCallback.Verify(call => call.Acknowledge(Status.REQUEUE));
    }

    private class TestErrorsErrorChannelHandler : IMessageHandler
    {
        private readonly CountdownEvent latch;

        public TestErrorsErrorChannelHandler(CountdownEvent latch)
        {
            this.latch = latch;
            ServiceName = $"{GetType().Name}@{GetHashCode()}";
        }

        public virtual string ServiceName { get; set; }

        public void HandleMessage(IMessage message)
        {
            latch.Signal();
        }
    }

    private class TestFuncMessageHandler : IMessageHandler
    {
        public int Count { get; set; }

        public TestFuncMessageHandler(Action<IMessage> action)
        {
            Act = action;
            ServiceName = $"{GetType().Name}@{GetHashCode()}";
        }

        public virtual string ServiceName { get; set; }

        public Action<IMessage> Act { get; }

        public void HandleMessage(IMessage message)
        {
            Count++;
            Act(message);
        }
    }

    private sealed class TestFuncMessageSource : IMessageSource
    {
        public TestFuncMessageSource(Func<IMessage> func)
        {
            Func = func;
        }

        public Func<IMessage> Func { get; }

        public IMessage Receive()
        {
            return Func();
        }
    }

    private sealed class TestMessageSource : IMessageSource
    {
        private readonly string payload;

        public TestMessageSource(string payload)
        {
            this.payload = payload;
        }

        public IMessage Receive()
        {
            return Message.Create(Encoding.UTF8.GetBytes(payload));
        }
    }

    private class TestConvertSimpleHandler : IMessageHandler
    {
        public object Payload { get; set; }

        public IMessage Message { get; set; }

        public TestConvertSimpleHandler()
        {
            ServiceName = $"{GetType().Name}@{GetHashCode()}";
        }

        public virtual string ServiceName { get; set; }

        public void HandleMessage(IMessage message)
        {
            Message = message;
            Payload = message.Payload;
        }
    }

    private class TestSimpleHandler : IMessageHandler
    {
        public int Count { get; set; }

        public TestSimpleHandler()
        {
            ServiceName = $"{GetType().Name}@{GetHashCode()}";
        }

        public virtual string ServiceName { get; set; }

        public void HandleMessage(IMessage message)
        {
            Assert.Equal("POLLED DATA", message.Payload);
            var contentType = message.Headers[MessageHeaders.CONTENT_TYPE];
            Assert.Equal("text/plain", contentType.ToString());
            Count++;
            if (Count == 1)
            {
                throw new Exception("test retry");
            }
        }
    }

    private sealed class TestRequeueChannelInterceptor : AbstractChannelInterceptor
    {
        public TestRequeueChannelInterceptor(Mock mock)
        {
            Mock = mock;
        }

        public Mock Mock { get; }

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            return MessageBuilder
                .FromMessage(message)
                .SetHeader(IntegrationMessageHeaderAccessor.ACKNOWLEDGMENT_CALLBACK, Mock.Object)
                .Build();
        }
    }

    private sealed class TestEmbededChannelInterceptor : AbstractChannelInterceptor
    {
        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            return MessageBuilder
                .WithPayload(Encoding.UTF8.GetString((byte[])message.Payload).ToUpper())
                .CopyHeaders(message.Headers)
                .Build();
        }
    }

    private sealed class TestSimpleChannelInterceptor : AbstractChannelInterceptor
    {
        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            return MessageBuilder
                .WithPayload(((string)message.Payload).ToUpper())
                .CopyHeaders(message.Headers)
                .Build();
        }
    }

    private sealed class FooType
    {
        public string Foo { get; set; }
    }
}
