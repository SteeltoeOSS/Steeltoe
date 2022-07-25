// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class TaskSchedulerChannelTest
{
    private readonly IServiceProvider _provider;

    public TaskSchedulerChannelTest()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void VerifyDifferentThread()
    {
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var latch = new CountdownEvent(1);
        var handler = new TestHandler(latch);
        channel.Subscribe(handler);
        channel.Send(Message.Create("test"));
        Assert.True(latch.Wait(1000));
        Assert.NotNull(handler.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler.Thread.ManagedThreadId);
    }

    [Fact]
    public void RoundRobinLoadBalancing()
    {
        var numberOfMessages = 12;
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var latch = new CountdownEvent(numberOfMessages);
        var handler1 = new TestHandler(latch);
        var handler2 = new TestHandler(latch);
        var handler3 = new TestHandler(latch);
        channel.Subscribe(handler1);
        channel.Subscribe(handler2);
        channel.Subscribe(handler3);
        for (var i = 0; i < numberOfMessages; i++)
        {
            channel.Send(Message.Create($"test-{i}"));
        }

        Assert.True(latch.Wait(3000));
        Assert.NotNull(handler1.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler1.Thread.ManagedThreadId);
        Assert.NotNull(handler2.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler2.Thread.ManagedThreadId);
        Assert.NotNull(handler3.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler3.Thread.ManagedThreadId);

        Assert.Equal(4, handler1.Count);
        Assert.Equal(4, handler2.Count);
        Assert.Equal(4, handler3.Count);
    }

    [Fact]
    public void VerifyFailoverWithLoadBalancing()
    {
        var numberOfMessages = 12;
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var latch = new CountdownEvent(numberOfMessages);
        var handler1 = new TestHandler(latch);
        var handler2 = new TestHandler(latch);
        var handler3 = new TestHandler(latch);
        channel.Subscribe(handler1);
        channel.Subscribe(handler2);
        channel.Subscribe(handler3);
        handler2.ShouldFail = true;
        for (var i = 0; i < numberOfMessages; i++)
        {
            channel.Send(Message.Create($"test-{i}"));
        }

        Assert.True(latch.Wait(3000));
        Assert.NotNull(handler1.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler1.Thread.ManagedThreadId);
        Assert.NotNull(handler2.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler2.Thread.ManagedThreadId);
        Assert.NotNull(handler3.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler3.Thread.ManagedThreadId);

        Assert.Equal(4, handler1.Count);
        Assert.Equal(0, handler2.Count);
        Assert.Equal(8, handler3.Count);
    }

    [Fact]
    public void VerifyFailoverWithoutLoadBalancing()
    {
        var numberOfMessages = 12;
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var latch = new CountdownEvent(numberOfMessages);
        var handler1 = new TestHandler(latch);
        var handler2 = new TestHandler(latch);
        var handler3 = new TestHandler(latch);
        channel.Subscribe(handler1);
        channel.Subscribe(handler2);
        channel.Subscribe(handler3);
        handler1.ShouldFail = true;
        for (var i = 0; i < numberOfMessages; i++)
        {
            channel.Send(Message.Create($"test-{i}"));
        }

        Assert.True(latch.Wait(3000));
        Assert.NotNull(handler1.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler1.Thread.ManagedThreadId);
        Assert.NotNull(handler2.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler2.Thread.ManagedThreadId);
        Assert.NotNull(handler3.Thread);
        Assert.NotEqual(Thread.CurrentThread.ManagedThreadId, handler3.Thread.ManagedThreadId);

        Assert.Equal(0, handler1.Count);
        Assert.Equal(8, handler2.Count);
        Assert.Equal(4, handler3.Count);
    }

    [Fact]
    public void InterceptorWithModifiedMessage()
    {
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);

        var mockHandler = new Mock<IMessageHandler>();
        var mockExpected = new Mock<IMessage>();
        var latch = new CountdownEvent(2);
        var interceptor = new BeforeHandleInterceptor(latch) { MessageToReturn = mockExpected.Object };
        channel.AddInterceptor(interceptor);
        channel.Subscribe(mockHandler.Object);
        channel.Send(Message.Create("foo"));
        Assert.True(latch.Wait(10000));
        mockHandler.Verify(h => h.HandleMessage(mockExpected.Object));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.AfterHandledInvoked);
    }

    [Fact]
    public void InterceptorWithException()
    {
        var channel = new TaskSchedulerChannel(_provider.GetService<IApplicationContext>(), TaskScheduler.Default);
        var message = Message.Create("foo");
        var mockHandler = new Mock<IMessageHandler>();

        var expected = new InvalidOperationException("Fake exception");
        var latch = new CountdownEvent(2);
        mockHandler.Setup(h => h.HandleMessage(message)).Throws(expected);
        var interceptor = new BeforeHandleInterceptor(latch);
        channel.AddInterceptor(interceptor);
        channel.Subscribe(mockHandler.Object);

        try
        {
            channel.Send(message);
        }
        catch (MessageDeliveryException actual)
        {
            Assert.Same(expected, actual.InnerException);
        }

        Assert.True(latch.Wait(3000));
        Assert.Equal(1, interceptor.Counter);
        Assert.True(interceptor.AfterHandledInvoked);
    }

    private sealed class TestHandler : IMessageHandler
    {
        public readonly CountdownEvent Latch;

        public int Count;

        public volatile Thread Thread;

        public volatile bool ShouldFail;

        public string ServiceName { get; set; } = nameof(TestHandler);

        public TestHandler(CountdownEvent latch)
        {
            Latch = latch;
        }

        public void HandleMessage(IMessage message)
        {
            Thread = Thread.CurrentThread;
            if (ShouldFail)
            {
                throw new Exception("intentional test failure");
            }

            Interlocked.Increment(ref Count);
            Latch.Signal();
        }
    }

    private sealed class BeforeHandleInterceptor : AbstractTaskSchedulerChannelInterceptor
    {
        public int Counter;
        public volatile bool AfterHandledInvoked;
        public IMessage MessageToReturn;
        private readonly CountdownEvent _latch;

        public BeforeHandleInterceptor()
        {
        }

        public BeforeHandleInterceptor(CountdownEvent latch)
        {
            _latch = latch;
        }

        public override IMessage BeforeHandled(IMessage message, IMessageChannel channel, IMessageHandler handler)
        {
            Assert.NotNull(message);
            Counter++;
            _latch?.Signal();
            return MessageToReturn ?? message;
        }

        public override void AfterMessageHandled(IMessage message, IMessageChannel channel, IMessageHandler handler, Exception ex)
        {
            AfterHandledInvoked = true;
            _latch?.Signal();
        }
    }
}
