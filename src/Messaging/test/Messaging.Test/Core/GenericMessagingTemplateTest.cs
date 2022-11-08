// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Test.Core;

public class GenericMessagingTemplateTest
{
    internal MessageChannelTemplate Template { get; }

    internal StubMessageChannel MessageChannel { get; }

    public GenericMessagingTemplateTest()
    {
        MessageChannel = new StubMessageChannel();

        Template = new MessageChannelTemplate
        {
            DefaultSendDestination = MessageChannel,
            DestinationResolver = new TestDestinationResolver(this)
        };
    }

    [Fact]
    public void SendWithTimeout()
    {
        IMessage sent = null;

        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;
        chanMock.Setup(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000))).Callback<IMessage, int>((m, _) => sent = m).Returns(true);

        IMessage message = MessageBuilder.WithPayload("request").SetHeader(MessageChannelTemplate.DefaultSendTimeoutHeader, 30000)
            .SetHeader(MessageChannelTemplate.DefaultReceiveTimeoutHeader, 1).Build();

        Template.Send(channel, message);

        chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        Assert.NotNull(sent);
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultSendTimeoutHeader));
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultReceiveTimeoutHeader));
    }

    [Fact]
    public async Task SendAsyncWithTimeout()
    {
        IMessage sent = null;

        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)))
            .Callback<IMessage, CancellationToken>((m, _) => sent = m).Returns(new ValueTask<bool>(true));

        IMessage message = MessageBuilder.WithPayload("request").SetHeader(MessageChannelTemplate.DefaultSendTimeoutHeader, 30000)
            .SetHeader(MessageChannelTemplate.DefaultReceiveTimeoutHeader, 1).Build();

        await Template.SendAsync(channel, message);

        chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)));
        Assert.NotNull(sent);
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultSendTimeoutHeader));
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultReceiveTimeoutHeader));
    }

    [Fact]
    public async Task SendAsyncWithTimeoutMutable()
    {
        IMessage sent = null;

        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)))
            .Callback<IMessage, CancellationToken>((m, _) => sent = m).Returns(new ValueTask<bool>(true));

        var accessor = new MessageHeaderAccessor
        {
            LeaveMutable = true
        };

        IMessage<string> message = Message.Create("request", accessor.MessageHeaders);
        accessor.SetHeader(MessageChannelTemplate.DefaultSendTimeoutHeader, 30000);
        await Template.SendAsync(channel, message);
        chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)));
        Assert.NotNull(sent);
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultSendTimeoutHeader));
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultReceiveTimeoutHeader));
    }

    [Fact]
    public void SendWithTimeoutMutable()
    {
        IMessage sent = null;

        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;
        chanMock.Setup(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000))).Callback<IMessage, int>((m, _) => sent = m).Returns(true);

        var accessor = new MessageHeaderAccessor
        {
            LeaveMutable = true
        };

        IMessage<string> message = Message.Create("request", accessor.MessageHeaders);
        accessor.SetHeader(MessageChannelTemplate.DefaultSendTimeoutHeader, 30000);
        Template.Send(channel, message);
        chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
        Assert.NotNull(sent);
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultSendTimeoutHeader));
        Assert.False(sent.Headers.ContainsKey(MessageChannelTemplate.DefaultReceiveTimeoutHeader));
    }

    [Fact]
    public void SendAndReceive()
    {
        var channel = new TaskSchedulerSubscribableChannel(TaskScheduler.Default);
        channel.Subscribe(new SendAndReceiveTestHandler());

        string actual = Template.ConvertSendAndReceive<string>(channel, "request");
        Assert.Equal("response", actual);
    }

    [Fact]
    public async Task SendAndReceiveAsync()
    {
        var channel = new TaskSchedulerSubscribableChannel(TaskScheduler.Default);
        channel.Subscribe(new SendAndReceiveTestHandler());

        string actual = await Template.ConvertSendAndReceiveAsync<string>(channel, "request");
        Assert.Equal("response", actual);
    }

    [Fact]
    public void SendAndReceiveTimeout()
    {
        var latch = new CountdownEvent(1);

        Template.ReceiveTimeout = 1;
        Template.SendTimeout = 30000;
        Template.ThrowExceptionOnLateReply = true;

        var handler = new LateReplierMessageHandler(latch);
        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000))).Callback<IMessage, int>((m, _) =>
        {
            Task.Run(() => handler.HandleMessage(m));
        }).Returns(true);

        string result = Template.ConvertSendAndReceive<string>(channel, "request");
        Assert.Null(result);
        Assert.True(latch.Wait(10000));
        Assert.Null(handler.Failure);

        chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
    }

    [Fact]
    public async Task SendAndReceiveAsyncTimeout()
    {
        var latch = new CountdownEvent(1);

        Template.ReceiveTimeout = 1;
        Template.SendTimeout = 30000;
        Template.ThrowExceptionOnLateReply = true;

        var handler = new LateReplierMessageHandler(latch);
        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)))
            .Callback<IMessage, CancellationToken>((m, t) =>
            {
                Task.Run(() => handler.HandleMessage(m), t);
            }).Returns(new ValueTask<bool>(true));

        string result = await Template.ConvertSendAndReceiveAsync<string>(channel, "request");
        Assert.Null(result);
        Assert.True(latch.Wait(10000));
        Assert.Null(handler.Failure);

        chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)));
    }

    [Fact]
    public void SendAndReceiveVariableTimeout()
    {
        var latch = new CountdownEvent(1);
        Template.ReceiveTimeout = 10000;
        Template.SendTimeout = 20000;
        Template.ThrowExceptionOnLateReply = true;

        var handler = new LateReplierMessageHandler(latch);
        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Callback<IMessage, int>((m, _) =>
        {
            Task.Run(() => handler.HandleMessage(m));
        }).Returns(true);

        IMessage message = MessageBuilder.WithPayload("request").SetHeader(MessageChannelTemplate.DefaultSendTimeoutHeader, 30000)
            .SetHeader(MessageChannelTemplate.DefaultReceiveTimeoutHeader, 1).Build();

        IMessage result = Template.SendAndReceive(channel, message);
        Assert.Null(result);
        Assert.True(latch.Wait(10000));
        Assert.Null(handler.Failure);

        chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
    }

    [Fact]
    public async Task SendAndReceiveAsyncVariableTimeout()
    {
        var latch = new CountdownEvent(1);
        Template.ReceiveTimeout = 10000;
        Template.SendTimeout = 20000;
        Template.ThrowExceptionOnLateReply = true;

        var handler = new LateReplierMessageHandler(latch);
        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)))
            .Callback<IMessage, CancellationToken>((m, t) =>
            {
                Task.Run(() => handler.HandleMessage(m), t);
            }).Returns(new ValueTask<bool>(true));

        IMessage message = MessageBuilder.WithPayload("request").SetHeader(MessageChannelTemplate.DefaultSendTimeoutHeader, 30000)
            .SetHeader(MessageChannelTemplate.DefaultReceiveTimeoutHeader, 1).Build();

        IMessage result = await Template.SendAndReceiveAsync(channel, message);
        Assert.Null(result);
        Assert.True(latch.Wait(10000));
        Assert.Null(handler.Failure);

        chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)));
    }

    [Fact]
    public void SendAndReceiveVariableTimeoutCustomHeaders()
    {
        var latch = new CountdownEvent(1);
        Template.ReceiveTimeout = 10000;
        Template.SendTimeout = 20000;
        Template.ThrowExceptionOnLateReply = true;
        Template.SendTimeoutHeader = "sto";
        Template.ReceiveTimeoutHeader = "rto";

        var handler = new LateReplierMessageHandler(latch);
        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Callback<IMessage, int>((m, _) =>
        {
            Task.Run(() => handler.HandleMessage(m));
        }).Returns(true);

        IMessage message = MessageBuilder.WithPayload("request").SetHeader("sto", 30000).SetHeader("rto", 1).Build();

        IMessage result = Template.SendAndReceive(channel, message);
        Assert.Null(result);
        Assert.True(latch.Wait(10000));
        Assert.Null(handler.Failure);

        chanMock.Verify(chan => chan.Send(It.IsAny<IMessage>(), It.Is<int>(i => i == 30000)));
    }

    [Fact]
    public async Task SendAndReceiveAsyncVariableTimeoutCustomHeaders()
    {
        var latch = new CountdownEvent(1);
        Template.ReceiveTimeout = 10000;
        Template.SendTimeout = 20000;
        Template.ThrowExceptionOnLateReply = true;
        Template.SendTimeoutHeader = "sto";
        Template.ReceiveTimeoutHeader = "rto";

        var handler = new LateReplierMessageHandler(latch);
        var chanMock = new Mock<ISubscribableChannel>();
        ISubscribableChannel channel = chanMock.Object;

        chanMock.Setup(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)))
            .Callback<IMessage, CancellationToken>((m, t) =>
            {
                Task.Run(() => handler.HandleMessage(m), t);
            }).Returns(new ValueTask<bool>(true));

        IMessage message = MessageBuilder.WithPayload("request").SetHeader("sto", 30000).SetHeader("rto", 1).Build();

        IMessage result = await Template.SendAndReceiveAsync(channel, message);
        Assert.Null(result);
        Assert.True(latch.Wait(10000));
        Assert.Null(handler.Failure);

        chanMock.Verify(chan => chan.SendAsync(It.IsAny<IMessage>(), It.Is<CancellationToken>(t => !t.IsCancellationRequested)));
    }

    internal sealed class LateReplierMessageHandler : IMessageHandler
    {
        public CountdownEvent Latch { get; }
        public Exception Failure { get; private set; }

        public string ServiceName { get; set; } = nameof(LateReplierMessageHandler);

        public LateReplierMessageHandler(CountdownEvent latch)
        {
            Latch = latch;
        }

        public void HandleMessage(IMessage message)
        {
            try
            {
                Thread.Sleep(1000);
                var replyChannel = (IMessageChannel)message.Headers.ReplyChannel;
                replyChannel.Send(Message.Create("response"));
                Failure = new InvalidOperationException("Expected exception");
            }
            catch (MessageDeliveryException ex)
            {
                const string expected = "Reply message received but the receiving thread has exited due to a timeout";
                string actual = ex.Message;

                if (!expected.Equals(actual))
                {
                    Failure = new InvalidOperationException($"Unexpected error: '{actual}'");
                }
            }
            catch (Exception e)
            {
                Failure = e;
            }
            finally
            {
                Latch.Signal();
            }
        }
    }

    internal sealed class SendAndReceiveTestHandler : IMessageHandler
    {
        public string ServiceName { get; set; } = nameof(SendAndReceiveTestHandler);

        public void HandleMessage(IMessage message)
        {
            var replyChannel = (IMessageChannel)message.Headers.ReplyChannel;
            replyChannel.Send(Message.Create("response"));
        }
    }

    internal sealed class TestDestinationResolver : IDestinationResolver<IMessageChannel>
    {
        private readonly GenericMessagingTemplateTest _test;

        public TestDestinationResolver(GenericMessagingTemplateTest test)
        {
            _test = test;
        }

        public IMessageChannel ResolveDestination(string name)
        {
            return _test.MessageChannel;
        }

        object IDestinationResolver.ResolveDestination(string name)
        {
            return ResolveDestination(name);
        }
    }
}
