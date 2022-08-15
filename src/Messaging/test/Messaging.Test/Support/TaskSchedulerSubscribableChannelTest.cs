// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Xunit;

namespace Steeltoe.Messaging.Support.Test;

public class TaskSchedulerSubscribableChannelTest
{
    internal readonly TaskSchedulerSubscribableChannel Channel;
    internal readonly object Payload;
    internal readonly IMessage Message;

    internal IMessageHandler Handler;

    public TaskSchedulerSubscribableChannelTest()
    {
        Channel = new TaskSchedulerSubscribableChannel();
        Payload = new object();
        Message = MessageBuilder.WithPayload(Payload).Build();
    }

    [Fact]
    public void MessageMustNotBeNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Channel.Send(null));
        Assert.Contains("message", ex.Message);
    }

    [Fact]
    public void SendNoInterceptors()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(Message));
    }

    [Fact]
    public void SendWithoutScheduler()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor = new BeforeHandleInterceptor();
        Channel.AddInterceptor(interceptor);
        Channel.Subscribe(Handler);
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(Message));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    [Fact]
    public void SendWithScheduler()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor = new BeforeHandleInterceptor();
        var scheduler = new TestScheduler();
        var testChannel = new TaskSchedulerSubscribableChannel(scheduler);
        testChannel.AddInterceptor(interceptor);
        testChannel.Subscribe(Handler);
        testChannel.Send(Message);
        Assert.True(scheduler.WasTaskScheduled);
        mock.Verify(h => h.HandleMessage(Message));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    [Fact]
    public void SubscribeTwice()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Assert.True(Channel.Subscribe(Handler));
        Assert.False(Channel.Subscribe(Handler));
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Once);
    }

    [Fact]
    public void UnsubscribeTwice()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        Assert.True(Channel.Unsubscribe(Handler));
        Assert.False(Channel.Unsubscribe(Handler));
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Never);
    }

    [Fact]
    public void FailurePropagates()
    {
        var ex = new Exception("My exception");
        var mock = new Mock<IMessageHandler>();
        mock.Setup(h => h.HandleMessage(Message)).Throws(ex);
        Handler = mock.Object;

        var mock2 = new Mock<IMessageHandler>();
        IMessageHandler secondHandler = mock2.Object;

        Channel.Subscribe(Handler);
        Channel.Subscribe(secondHandler);
        bool exceptionThrown = false;

        try
        {
            Channel.Send(Message);
        }
        catch (MessageDeliveryException actualException)
        {
            exceptionThrown = true;
            Assert.Equal(ex, actualException.InnerException);
            Assert.Contains("My exception", actualException.InnerException.Message);
        }

        Assert.True(exceptionThrown);
        mock2.Verify(h => h.HandleMessage(Message), Times.Never);
    }

    [Fact]
    public void ConcurrentModification()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var unsubscribeHandler = new UnsubscribeHandler(this);
        Channel.Subscribe(unsubscribeHandler);
        Channel.Subscribe(Handler);
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Once);
    }

    [Fact]
    public void InterceptorWithModifiedMessage()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var mock2 = new Mock<IMessage>();
        IMessage expected = mock2.Object;

        var interceptor = new BeforeHandleInterceptor
        {
            MessageToReturn = expected
        };

        Channel.AddInterceptor(interceptor);
        Channel.Subscribe(Handler);
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(expected), Times.Once);

        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    [Fact]
    public void InterceptorWithNull()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor1 = new BeforeHandleInterceptor();
        var interceptor2 = new NullReturningBeforeHandleInterceptor();
        Channel.AddInterceptor(interceptor1);
        Channel.AddInterceptor(interceptor2);
        Channel.Subscribe(Handler);
        Channel.Send(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Never);
        Assert.Equal(1, interceptor1.Counter);
        Assert.Equal(1, interceptor2.Counter);
        Assert.True(interceptor1.WasAfterHandledInvoked);
    }

    [Fact]
    public void InterceptorWithException()
    {
        var expected = new Exception("Fake exception");
        var mock = new Mock<IMessageHandler>();
        mock.Setup(h => h.HandleMessage(Message)).Throws(expected);
        Handler = mock.Object;

        var interceptor = new BeforeHandleInterceptor();
        Channel.AddInterceptor(interceptor);
        Channel.Subscribe(Handler);
        bool exceptionThrown = false;

        try
        {
            Channel.Send(Message);
        }
        catch (MessageDeliveryException actual)
        {
            exceptionThrown = true;
            Assert.Same(expected, actual.InnerException);
        }

        Assert.True(exceptionThrown);
        mock.Verify(h => h.HandleMessage(Message), Times.Once);
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    internal sealed class UnsubscribeHandler : IMessageHandler
    {
        private readonly TaskSchedulerSubscribableChannelTest _test;
        private readonly TaskSchedulerSubscribableChannelWriterTest _test2;

        public string ServiceName { get; set; } = nameof(UnsubscribeHandler);

        public UnsubscribeHandler(TaskSchedulerSubscribableChannelTest test)
        {
            _test = test;
        }

        public UnsubscribeHandler(TaskSchedulerSubscribableChannelWriterTest test)
        {
            _test2 = test;
        }

        public void HandleMessage(IMessage message)
        {
            _test?.Channel.Unsubscribe(_test.Handler);
            _test2?.Channel.Unsubscribe(_test2.Handler);
        }
    }

    internal sealed class TestScheduler : TaskScheduler
    {
        public bool WasTaskScheduled;

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new NotImplementedException();
        }

        protected override void QueueTask(Task task)
        {
            WasTaskScheduled = true;
            TryExecuteTask(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            WasTaskScheduled = true;
            TryExecuteTask(task);
            return true;
        }
    }

    internal abstract class AbstractTestInterceptor : AbstractTaskSchedulerChannelInterceptor
    {
        private volatile int _counter;

        private volatile bool _afterHandledInvoked;

        public int Counter => _counter;

        public bool WasAfterHandledInvoked => _afterHandledInvoked;

        public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
        {
            Assert.NotNull(message);
            Interlocked.Increment(ref _counter);
            return message;
        }

        public override void AfterMessageHandled(IMessage message, IMessageChannel channel, IMessageHandler handler, Exception exception)
        {
            _afterHandledInvoked = true;
        }
    }

    internal sealed class BeforeHandleInterceptor : AbstractTestInterceptor
    {
        public IMessage MessageToReturn { get; set; }

        public Exception ExceptionToRaise { get; set; }

        public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
        {
            base.BeforeHandled(message, channel, handler);

            if (ExceptionToRaise != null)
            {
                throw ExceptionToRaise;
            }

            return MessageToReturn ?? message;
        }
    }

    internal sealed class NullReturningBeforeHandleInterceptor : AbstractTestInterceptor
    {
        public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
        {
            base.BeforeHandled(message, channel, handler);
            return null;
        }
    }
}
