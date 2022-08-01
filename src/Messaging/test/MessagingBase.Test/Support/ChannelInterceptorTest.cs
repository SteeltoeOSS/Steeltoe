// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Steeltoe.Messaging.Support.Test;

public class ChannelInterceptorTest
{
    private readonly TaskSchedulerSubscribableChannel _channel;

    private readonly TestMessageHandler _messageHandler;

    public ChannelInterceptorTest()
    {
        _channel = new TaskSchedulerSubscribableChannel();
        _messageHandler = new TestMessageHandler();
        _channel.Subscribe(_messageHandler);
    }

    [Fact]
    public void PreSendInterceptorReturningModifiedMessage()
    {
        var expected = new Mock<IMessage>().Object;
        var interceptor = new PreSendInterceptor
        {
            MessageToReturn = expected
        };
        _channel.AddInterceptor(interceptor);
        _channel.Send(MessageBuilder.WithPayload("test").Build());

        Assert.Single(_messageHandler.Messages);
        var result = _messageHandler.Messages[0];

        Assert.NotNull(result);
        Assert.Same(expected, result);
        Assert.True(interceptor.WasAfterCompletionInvoked);
    }

    [Fact]
    public void PreSendInterceptorReturningNull()
    {
        var interceptor1 = new PreSendInterceptor();
        var interceptor2 = new NullReturningPreSendInterceptor();
        _channel.AddInterceptor(interceptor1);
        _channel.AddInterceptor(interceptor2);
        var message = MessageBuilder.WithPayload("test").Build();
        _channel.Send(message);

        Assert.Equal(1, interceptor1.Counter);
        Assert.Equal(1, interceptor2.Counter);
        Assert.Empty(_messageHandler.Messages);
        Assert.True(interceptor1.WasAfterCompletionInvoked);
        Assert.False(interceptor2.WasAfterCompletionInvoked);
    }

    [Fact]
    public void PostSendInterceptorMessageWasSent()
    {
        var interceptor = new PostSendInterceptorMessageWasSentChannelInterceptor(_channel);
        _channel.AddInterceptor(interceptor);

        _channel.Send(MessageBuilder.WithPayload("test").Build());
        Assert.True(interceptor.PreSendInvoked);
        Assert.True(interceptor.CompletionInvoked);
    }

    [Fact]
    public void PostSendInterceptorMessageWasNotSent()
    {
        AbstractMessageChannel testChannel = new PostSendInterceptorMessageWasNotSentChannel();
        var interceptor = new PostSendInterceptorMessageWasNotSentInterceptor(testChannel);

        testChannel.AddInterceptor(interceptor);

        testChannel.Send(MessageBuilder.WithPayload("test").Build());
        Assert.True(interceptor.PreSendInvoked);
        Assert.True(interceptor.CompletionInvoked);
    }

    [Fact]
    public void AfterCompletionWithSendException()
    {
        AbstractMessageChannel testChannel = new AfterCompletionWithSendExceptionChannel();

        var interceptor1 = new PreSendInterceptor();
        var interceptor2 = new PreSendInterceptor();
        testChannel.AddInterceptor(interceptor1);
        testChannel.AddInterceptor(interceptor2);
        try
        {
            testChannel.Send(MessageBuilder.WithPayload("test").Build());
        }
        catch (Exception ex)
        {
            Assert.Equal("Simulated exception", ex.InnerException.Message);
        }

        Assert.True(interceptor1.WasAfterCompletionInvoked);
        Assert.True(interceptor2.WasAfterCompletionInvoked);
    }

    [Fact]
    public void AfterCompletionWithPreSendException()
    {
        var interceptor1 = new PreSendInterceptor();
        var interceptor2 = new PreSendInterceptor
        {
            ExceptionToRaise = new Exception("Simulated exception")
        };
        _channel.AddInterceptor(interceptor1);
        _channel.AddInterceptor(interceptor2);
        try
        {
            _channel.Send(MessageBuilder.WithPayload("test").Build());
        }
        catch (Exception ex)
        {
            Assert.Equal("Simulated exception", ex.InnerException.Message);
        }

        Assert.True(interceptor1.WasAfterCompletionInvoked);
        Assert.False(interceptor2.WasAfterCompletionInvoked);
    }

    // internal sealed class AfterCompletionWithSendExceptionChannel : AbstractMessageChannel
    // {
    //    protected override bool SendInternal(IMessage message, long timeout)
    //    {
    //        throw new Exception("Simulated exception");
    //    }

    // protected override Task<bool> SendInternalAsync(IMessage message, CancellationToken cancellation = default)
    //    {
    //        throw new NotImplementedException();
    //    }
    // }
    internal sealed class AfterCompletionWithSendExceptionChannel : AbstractMessageChannel
    {
        public AfterCompletionWithSendExceptionChannel()
        {
            Writer = new AfterCompletionWithSendExceptionChannelWriter(this);
        }

        protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
        {
            throw new Exception("Simulated exception");
        }
    }

    internal sealed class AfterCompletionWithSendExceptionChannelWriter : AbstractMessageChannelWriter
    {
        public AfterCompletionWithSendExceptionChannelWriter(AbstractMessageChannel channel, ILogger logger = null)
            : base(channel, logger)
        {
        }
    }

    // internal sealed class PostSendInterceptorMessageWasNotSentChannel : AbstractMessageChannel
    // {
    //    protected override bool SendInternal(IMessage message, long timeout)
    //    {
    //        return false;
    //    }

    // protected override Task<bool> SendInternalAsync(IMessage message, CancellationToken cancellation = default)
    //    {
    //        throw new NotImplementedException();
    //    }
    // }
    internal sealed class PostSendInterceptorMessageWasNotSentChannel : AbstractMessageChannel
    {
        public PostSendInterceptorMessageWasNotSentChannel()
        {
            Writer = new PostSendInterceptorMessageWasNotSentChannelWriter(this);
        }

        protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
        {
            return false;
        }
    }

    internal sealed class PostSendInterceptorMessageWasNotSentChannelWriter : AbstractMessageChannelWriter
    {
        public PostSendInterceptorMessageWasNotSentChannelWriter(AbstractMessageChannel channel, ILogger logger = null)
            : base(channel, logger)
        {
        }
    }

    internal sealed class PostSendInterceptorMessageWasNotSentInterceptor : AbstractChannelInterceptor
    {
        public bool PreSendInvoked;
        public bool CompletionInvoked;
        public IMessageChannel ExpectedChannel;

        public PostSendInterceptorMessageWasNotSentInterceptor(IMessageChannel expectedChannel)
        {
            this.ExpectedChannel = expectedChannel;
        }

        public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
            AssertInput(message, channel, sent);
            PreSendInvoked = true;
        }

        public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
            AssertInput(message, channel, sent);
            CompletionInvoked = true;
        }

        private void AssertInput(IMessage message, IMessageChannel channel, bool sent)
        {
            Assert.NotNull(message);
            Assert.NotNull(channel);
            Assert.Same(ExpectedChannel, channel);
            Assert.False(sent);
        }
    }

    internal sealed class PostSendInterceptorMessageWasSentChannelInterceptor : AbstractChannelInterceptor
    {
        public bool PreSendInvoked;
        public bool CompletionInvoked;
        public IMessageChannel ExpectedChannel;

        public PostSendInterceptorMessageWasSentChannelInterceptor(IMessageChannel expectedChannel)
        {
            this.ExpectedChannel = expectedChannel;
        }

        public override void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
            AssertInput(message, channel, sent);
            PreSendInvoked = true;
        }

        public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
            AssertInput(message, channel, sent);
            CompletionInvoked = true;
        }

        private void AssertInput(IMessage message, IMessageChannel channel, bool sent)
        {
            Assert.NotNull(message);
            Assert.NotNull(channel);
            Assert.Same(ExpectedChannel, channel);
            Assert.True(sent);
        }
    }

    internal sealed class TestMessageHandler : IMessageHandler
    {
        public List<IMessage> Messages { get; } = new ();

        public string ServiceName { get; set; } = nameof(TestMessageHandler);

        public void HandleMessage(IMessage message)
        {
            Messages.Add(message);
        }
    }

    internal abstract class AbstractTestInterceptor : AbstractTaskSchedulerChannelInterceptor
    {
        private volatile int _counter;

        private volatile bool _afterCompletionInvoked;

        public int Counter
        {
            get { return _counter; }
        }

        public bool WasAfterCompletionInvoked
        {
            get { return _afterCompletionInvoked; }
        }

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            Assert.NotNull(message);
            Interlocked.Increment(ref _counter);
            return message;
        }

        public override void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
            _afterCompletionInvoked = true;
        }
    }

    internal sealed class PreSendInterceptor : AbstractTestInterceptor
    {
        public IMessage MessageToReturn { get; set; }

        public Exception ExceptionToRaise { get; set; }

        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            base.PreSend(message, channel);
            if (ExceptionToRaise != null)
            {
                throw ExceptionToRaise;
            }

            return MessageToReturn ?? message;
        }
    }

    internal sealed class NullReturningPreSendInterceptor : AbstractTestInterceptor
    {
        public override IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            base.PreSend(message, channel);
            return null;
        }
    }
}
