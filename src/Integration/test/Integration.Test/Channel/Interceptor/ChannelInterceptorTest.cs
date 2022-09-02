// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Integration.Channel.Interceptor.Test;

public class ChannelInterceptorTest
{
    private readonly QueueChannel _channel;
    private readonly IServiceProvider _provider;

    public ChannelInterceptorTest()
    {
        var services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();
        _channel = new QueueChannel(_provider.GetService<IApplicationContext>());
    }

    [Fact]
    public void TestPreSendInterceptorReturnsMessage()
    {
        var interceptor = new PreSendReturnsMessageInterceptor();
        _channel.AddInterceptor(interceptor);
        _channel.Send(Message.Create("test"));
        IMessage result = _channel.Receive(0);
        Assert.NotNull(result);
        Assert.Equal("test", result.Payload);
        Assert.Equal(1, result.Headers[nameof(PreSendReturnsMessageInterceptor)]);
        Assert.True(interceptor.AfterCompletionInvoked);
    }

    [Fact]
    public void TestPreSendInterceptorReturnsNull()
    {
        var interceptor = new PreSendReturnsNullInterceptor();
        _channel.AddInterceptor(interceptor);
        IMessage message = Message.Create("test");
        _channel.Send(message);
        Assert.Equal(1, interceptor.Counter);

        Assert.True(_channel.RemoveInterceptor(interceptor));

        _channel.Send(Message.Create("TEST"));
        Assert.Equal(1, interceptor.Counter);

        IMessage result = _channel.Receive(0);
        Assert.NotNull(result);
        Assert.Equal("TEST", result.Payload);
    }

    [Fact]
    public void TestPostSendInterceptorWithSentMessage()
    {
        var interceptor = new TestPostSendInterceptorWithSentMessageInterceptor();
        _channel.AddInterceptor(interceptor);
        _channel.Send(Message.Create("test"));
        Assert.True(interceptor.Invoked);
    }

    [Fact]
    public void TestPostSendInterceptorWithUnsentMessage()
    {
        var singleItemChannel = new QueueChannel(_provider.GetService<IApplicationContext>(), 1);
        var interceptor = new TestPostSendInterceptorWithUnsentMessageInterceptor();
        singleItemChannel.AddInterceptor(interceptor);
        Assert.Equal(0, interceptor.InvokedCounter);
        Assert.Equal(0, interceptor.SentCounter);
        singleItemChannel.Send(Message.Create("test1"));
        Assert.Equal(1, interceptor.InvokedCounter);
        Assert.Equal(1, interceptor.SentCounter);
        singleItemChannel.Send(Message.Create("test2"), 0);
        Assert.Equal(2, interceptor.InvokedCounter);
        Assert.Equal(1, interceptor.SentCounter);
        Assert.NotNull(singleItemChannel.RemoveInterceptor(0));
        singleItemChannel.Send(Message.Create("test2"), 0);
        Assert.Equal(2, interceptor.InvokedCounter);
        Assert.Equal(1, interceptor.SentCounter);
    }

    [Fact]
    public void AfterCompletionWithSendException()
    {
        AbstractMessageChannel testChannel = new AfterCompletionWithSendExceptionChannel(_provider.GetService<IApplicationContext>());

        var interceptor1 = new AfterCompletionTestInterceptor();
        var interceptor2 = new AfterCompletionTestInterceptor();
        testChannel.AddInterceptor(interceptor1);
        testChannel.AddInterceptor(interceptor2);

        try
        {
            testChannel.Send(IntegrationMessageBuilder.WithPayload("test").Build());
        }
        catch (Exception ex)
        {
            Assert.Equal("Simulated exception", ex.InnerException.Message);
        }

        Assert.True(interceptor1.AfterCompletionInvoked);
        Assert.True(interceptor2.AfterCompletionInvoked);
    }

    [Fact]
    public void AfterCompletionWithPreSendException()
    {
        var interceptor1 = new AfterCompletionTestInterceptor();

        var interceptor2 = new AfterCompletionTestInterceptor
        {
            ExceptionToRaise = new Exception("Simulated exception")
        };

        _channel.AddInterceptor(interceptor1);
        _channel.AddInterceptor(interceptor2);

        try
        {
            _channel.Send(IntegrationMessageBuilder.WithPayload("test").Build());
        }
        catch (Exception ex)
        {
            Assert.Equal("Simulated exception", ex.InnerException.Message);
        }

        Assert.True(interceptor1.AfterCompletionInvoked);
        Assert.False(interceptor2.AfterCompletionInvoked);
    }

    [Fact]
    public void TestPreReceiveInterceptorReturnsTrue()
    {
        var interceptor = new PreReceiveReturnsTrueInterceptor();
        _channel.AddInterceptor(interceptor);
        IMessage<string> message = Message.Create("test");
        _channel.Send(message);
        IMessage result = _channel.Receive(0);
        Assert.Equal(1, interceptor.Counter);
        Assert.NotNull(result);
        Assert.True(interceptor.AfterCompletionInvoked);
    }

    [Fact]
    public void TestPreReceiveInterceptorReturnsFalse()
    {
        var interceptor = new PreReceiveReturnsFalseInterceptor();
        _channel.AddInterceptor(interceptor);
        IMessage<string> message = Message.Create("test");
        _channel.Send(message);
        IMessage result = _channel.Receive(0);
        Assert.Equal(1, interceptor.Counter);
        Assert.Null(result);
    }

    [Fact]
    public void TestPostReceiveInterceptor()
    {
        var interceptor = new TestPostReceiveInterceptorInterceptor();
        _channel.AddInterceptor(interceptor);

        _channel.Receive(0);
        Assert.Equal(0, interceptor.Counter);
        _channel.Send(Message.Create("test"));
        IMessage result = _channel.Receive(0);
        Assert.NotNull(result);
        Assert.Equal(1, interceptor.Counter);
    }

    [Fact]
    public void AfterCompletionWithReceiveException()
    {
        var interceptor1 = new PreReceiveReturnsTrueInterceptor();

        var interceptor2 = new PreReceiveReturnsTrueInterceptor
        {
            ExceptionToRaise = new Exception("Simulated exception")
        };

        _channel.AddInterceptor(interceptor1);
        _channel.AddInterceptor(interceptor2);

        try
        {
            _channel.Receive(0);
        }
        catch (Exception ex)
        {
            Assert.Equal("Simulated exception", ex.Message);
        }

        Assert.True(interceptor1.AfterCompletionInvoked);
        Assert.False(interceptor2.AfterCompletionInvoked);
    }

    public class TestPostReceiveInterceptorInterceptor : AbstractChannelInterceptor
    {
        private int _counter;

        public int Counter => _counter;

        public override IMessage PostReceive(IMessage message, IMessageChannel channel)
        {
            Assert.NotNull(channel);
            Interlocked.Increment(ref _counter);
            return message;
        }
    }

    public class AfterCompletionWithSendExceptionChannel : AbstractMessageChannel
    {
        public AfterCompletionWithSendExceptionChannel(IApplicationContext context, ILogger logger = null)
            : base(context, logger)
        {
        }

        protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
        {
            throw new Exception("Simulated exception");
        }
    }

    public class TestPostSendInterceptorWithUnsentMessageInterceptor : AbstractChannelInterceptor
    {
        private int _invokedCounter;
        private int _sentCounter;

        public int InvokedCounter => _invokedCounter;
        public int SentCounter => _sentCounter;

        public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
            Assert.NotNull(message);
            Assert.NotNull(channel);

            if (sent)
            {
                Interlocked.Increment(ref _sentCounter);
            }

            Interlocked.Increment(ref _invokedCounter);
        }
    }

    private sealed class TestPostSendInterceptorWithSentMessageInterceptor : AbstractChannelInterceptor
    {
        public bool Invoked { get; private set; }

        public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
            Assert.NotNull(message);
            Assert.NotNull(channel);
            Assert.True(sent);
            Invoked = true;
        }
    }

    private sealed class PreSendReturnsMessageInterceptor : AbstractChannelInterceptor
    {
        private int _counter;
        private volatile bool _afterCompletionInvoked;

        public bool AfterCompletionInvoked => _afterCompletionInvoked;

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            Assert.NotNull(message);
            int value = Interlocked.Increment(ref _counter);
            return IntegrationMessageBuilder.FromMessage(message).SetHeader(GetType().Name, value).Build();
        }

        public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
            _afterCompletionInvoked = true;
        }
    }

    private sealed class PreSendReturnsNullInterceptor : AbstractChannelInterceptor
    {
        private int _counter;

        public int Counter => _counter;

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            Assert.NotNull(message);
            Interlocked.Increment(ref _counter);
            return null;
        }
    }

    private sealed class AfterCompletionTestInterceptor : AbstractChannelInterceptor
    {
        private int _counter;
        private volatile bool _afterCompletionInvoked;

        public Exception ExceptionToRaise { get; set; }
        public bool AfterCompletionInvoked => _afterCompletionInvoked;

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            Assert.NotNull(message);
            Interlocked.Increment(ref _counter);

            if (ExceptionToRaise != null)
            {
                throw ExceptionToRaise;
            }

            return message;
        }

        public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
            _afterCompletionInvoked = true;
        }
    }

    private sealed class PreReceiveReturnsTrueInterceptor : AbstractChannelInterceptor
    {
        private int _counter;
        private volatile bool _afterCompletionInvoked;

        public Exception ExceptionToRaise { get; set; }
        public int Counter => _counter;
        public bool AfterCompletionInvoked => _afterCompletionInvoked;

        public override bool PreReceive(IMessageChannel channel)
        {
            Interlocked.Increment(ref _counter);

            if (ExceptionToRaise != null)
            {
                throw ExceptionToRaise;
            }

            return true;
        }

        public override void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception exception)
        {
            _afterCompletionInvoked = true;
        }
    }

    private sealed class PreReceiveReturnsFalseInterceptor : AbstractChannelInterceptor
    {
        private int _counter;

        public int Counter => _counter;

        public override bool PreReceive(IMessageChannel channel)
        {
            Interlocked.Increment(ref _counter);
            return false;
        }
    }
}
