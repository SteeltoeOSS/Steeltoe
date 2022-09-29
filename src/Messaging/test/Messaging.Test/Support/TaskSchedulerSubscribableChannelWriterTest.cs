// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Xunit;
using static Steeltoe.Messaging.Support.Test.TaskSchedulerSubscribableChannelTest;

namespace Steeltoe.Messaging.Support.Test;

public class TaskSchedulerSubscribableChannelWriterTest
{
    internal TaskSchedulerSubscribableChannel Channel { get; }
    internal object Payload { get; }
    internal IMessage Message { get; }
    internal IMessageHandler Handler { get; private set; }

    public TaskSchedulerSubscribableChannelWriterTest()
    {
        Channel = new TaskSchedulerSubscribableChannel();
        Payload = new object();
        Message = MessageBuilder.WithPayload(Payload).Build();
    }

    [Fact]
    public async Task MessageMustNotBeNull()
    {
        Exception exception = null;

        try
        {
            await Channel.Writer.WriteAsync(null);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        Assert.NotNull(exception);
        Assert.Contains("message", exception.Message);
    }

    [Fact]
    public async ValueTask WriteAsyncNoInterceptors()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(Message));
    }

    [Fact]
    public async ValueTask WriteAsyncWithoutScheduler()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor = new BeforeHandleInterceptor();
        Channel.AddInterceptor(interceptor);
        Channel.Subscribe(Handler);
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(Message));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    [Fact]
    public async ValueTask WriteAsyncWithScheduler()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor = new BeforeHandleInterceptor();
        var scheduler = new TestScheduler();
        var testChannel = new TaskSchedulerSubscribableChannel(scheduler);
        testChannel.AddInterceptor(interceptor);
        testChannel.Subscribe(Handler);
        await testChannel.Writer.WriteAsync(Message);
        Assert.True(scheduler.WasTaskScheduled);
        mock.Verify(h => h.HandleMessage(Message));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    [Fact]
    public async ValueTask SubscribeTwice()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Assert.True(Channel.Subscribe(Handler));
        Assert.False(Channel.Subscribe(Handler));
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeTwice()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        Assert.True(Channel.Unsubscribe(Handler));
        Assert.False(Channel.Unsubscribe(Handler));
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Never);
    }

    [Fact]
    public async ValueTask FailurePropagates()
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
            await Channel.Writer.WriteAsync(Message);
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
    public async ValueTask ConcurrentModification()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var unsubscribeHandler = new UnsubscribeHandler(this);
        Channel.Subscribe(unsubscribeHandler);
        Channel.Subscribe(Handler);
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Once);
    }

    [Fact]
    public async ValueTask InterceptorWithModifiedMessage()
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
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(expected), Times.Once);

        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }

    [Fact]
    public async ValueTask InterceptorWithNull()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor1 = new BeforeHandleInterceptor();
        var interceptor2 = new NullReturningBeforeHandleInterceptor();
        Channel.AddInterceptor(interceptor1);
        Channel.AddInterceptor(interceptor2);
        Channel.Subscribe(Handler);
        await Channel.Writer.WriteAsync(Message);
        mock.Verify(h => h.HandleMessage(Message), Times.Never);
        Assert.Equal(1, interceptor1.Counter);
        Assert.Equal(1, interceptor2.Counter);
        Assert.True(interceptor1.WasAfterHandledInvoked);
    }

    [Fact]
    public async ValueTask InterceptorWithException()
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
            await Channel.Writer.WriteAsync(Message);
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

    [Fact]
    public async ValueTask TestWaitToWriteAsync()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        Assert.True(await Channel.Writer.WaitToWriteAsync());
        Channel.Unsubscribe(Handler);
        Assert.False(await Channel.Writer.WaitToWriteAsync());
    }

    [Fact]
    public void TestTryComplete()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        Assert.False(Channel.Writer.TryComplete());
    }

    [Fact]
    public void TryWriteNoInterceptors()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        Channel.Subscribe(Handler);
        Assert.True(Channel.Writer.TryWrite(Message));
        mock.Verify(h => h.HandleMessage(Message));
    }

    [Fact]
    public void TryWriteWithInterceptors()
    {
        var mock = new Mock<IMessageHandler>();
        Handler = mock.Object;
        var interceptor = new BeforeHandleInterceptor();
        Channel.AddInterceptor(interceptor);
        Channel.Subscribe(Handler);
        Assert.True(Channel.Writer.TryWrite(Message));
        mock.Verify(h => h.HandleMessage(Message));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.WasAfterHandledInvoked);
    }
}
