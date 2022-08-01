// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Dispatcher.Test;

public class RoundRobinDispatcherConcurrentTest
{
    private const int TotalExecutions = 40;

    private readonly UnicastingDispatcher _dispatcher;

    private readonly Mock<IMessage> _messageMock = new ();

    private readonly Mock<IMessageHandler> _handlerMock1 = new ();

    private readonly Mock<IMessageHandler> _handlerMock2 = new ();

    private readonly Mock<IMessageHandler> _handlerMock3 = new ();

    private readonly Mock<IMessageHandler> _handlerMock4 = new ();

    private readonly IServiceProvider _provider;

    public RoundRobinDispatcherConcurrentTest()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
        _provider = services.BuildServiceProvider();
        _dispatcher = new UnicastingDispatcher(_provider.GetService<IApplicationContext>())
        {
            LoadBalancingStrategy = new RoundRobinLoadBalancingStrategy()
        };
    }

    [Fact]
    public void NoHandlerExhaustion()
    {
        _dispatcher.AddHandler(_handlerMock1.Object);
        _dispatcher.AddHandler(_handlerMock2.Object);
        _dispatcher.AddHandler(_handlerMock3.Object);
        _dispatcher.AddHandler(_handlerMock4.Object);

        var start = new CountdownEvent(1);
        var allDone = new CountdownEvent(TotalExecutions);
        var message = _messageMock.Object;
        var failed = false;
        void MessageSenderTask()
        {
            start.Wait();

            if (!_dispatcher.Dispatch(message))
            {
                failed = true;
            }

            allDone.Signal();
        }

        for (var i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start.Signal();
        Assert.True(allDone.Wait(10000));
        Assert.False(failed);
        _handlerMock1.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(TotalExecutions / 4));
        _handlerMock2.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(TotalExecutions / 4));
        _handlerMock3.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(TotalExecutions / 4));
        _handlerMock4.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(TotalExecutions / 4));
    }

    [Fact]
    public void UnlockOnFailure()
    {
        // dispatcher has no subscribers (shouldn't lead to deadlock)
        var start = new CountdownEvent(1);
        var allDone = new CountdownEvent(TotalExecutions);
        var message = _messageMock.Object;
        void MessageSenderTask()
        {
            start.Wait();

            try
            {
                _dispatcher.Dispatch(message);
                throw new Exception("this shouldn't happen");
            }
            catch (MessagingException)
            {
                // expected
            }

            allDone.Signal();
        }

        for (var i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start.Signal();
        Assert.True(allDone.Wait(10000));
    }

    [Fact]
    public void NoHandlerSkipUnderConcurrentFailureWithFailover()
    {
        _dispatcher.AddHandler(_handlerMock1.Object);
        _dispatcher.AddHandler(_handlerMock2.Object);
        _handlerMock1.Setup(h => h.HandleMessage(_messageMock.Object)).Throws(new MessageRejectedException(_messageMock.Object, null));
        var start = new CountdownEvent(1);
        var allDone = new CountdownEvent(TotalExecutions);
        var message = _messageMock.Object;
        var failed = false;
        void MessageSenderTask()
        {
            start.Wait();

            if (!_dispatcher.Dispatch(message))
            {
                failed = true;
            }
            else
            {
                allDone.Signal();
            }
        }

        for (var i = 0; i < TotalExecutions; i++)
        {
            Task.Run(MessageSenderTask);
        }

        start.Signal();
        Assert.True(allDone.Wait(10000));
        Assert.False(failed);
        _handlerMock1.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(TotalExecutions / 2));
        _handlerMock2.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(TotalExecutions));
    }
}
