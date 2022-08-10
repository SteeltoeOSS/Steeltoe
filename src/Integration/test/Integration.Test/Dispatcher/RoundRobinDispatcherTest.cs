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

public class RoundRobinDispatcherTest
{
    private readonly UnicastingDispatcher _dispatcher;

    private readonly Mock<IMessage> _messageMock = new();

    private readonly Mock<IMessageHandler> _handlerMock = new();

    private readonly Mock<IMessageHandler> _differentHandlerMock = new();

    private readonly IServiceProvider _provider;

    public RoundRobinDispatcherTest()
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
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
    public void DispatchMessageWithSingleHandler()
    {
        _dispatcher.AddHandler(_handlerMock.Object);
        _dispatcher.Dispatch(_messageMock.Object);
        _handlerMock.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void DifferentHandlerInvokedOnSecondMessage()
    {
        _dispatcher.AddHandler(_handlerMock.Object);
        _dispatcher.AddHandler(_differentHandlerMock.Object);
        _dispatcher.Dispatch(_messageMock.Object);
        _dispatcher.Dispatch(_messageMock.Object);
        _handlerMock.Verify(h => h.HandleMessage(_messageMock.Object));
        _differentHandlerMock.Verify(h => h.HandleMessage(_messageMock.Object));
    }

    [Fact]
    public void MultipleCyclesThroughHandlers()
    {
        _dispatcher.AddHandler(_handlerMock.Object);
        _dispatcher.AddHandler(_differentHandlerMock.Object);

        for (int i = 0; i < 7; i++)
        {
            _dispatcher.Dispatch(_messageMock.Object);
        }

        _handlerMock.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(4));
        _differentHandlerMock.Verify(h => h.HandleMessage(_messageMock.Object), Times.Exactly(3));
    }

    [Fact]
    public void CurrentHandlerIndexOverFlow()
    {
        _dispatcher.AddHandler(_handlerMock.Object);
        _dispatcher.AddHandler(_differentHandlerMock.Object);
        var balancer = _dispatcher.LoadBalancingStrategy as RoundRobinLoadBalancingStrategy;
        balancer.CurrentHandlerIndex = int.MaxValue - 5;

        for (int i = 0; i < 40; i++)
        {
            _dispatcher.Dispatch(_messageMock.Object);
        }

        _handlerMock.Verify(h => h.HandleMessage(_messageMock.Object), Times.AtLeast(18));
        _differentHandlerMock.Verify(h => h.HandleMessage(_messageMock.Object), Times.AtLeast(18));
    }

    [Fact]
    public void TestExceptionEnhancement()
    {
        _dispatcher.AddHandler(_handlerMock.Object);
        _handlerMock.Setup(h => h.HandleMessage(_messageMock.Object)).Throws(new MessagingException("Mock Exception"));
        var ex = Assert.Throws<MessageDeliveryException>(() => _dispatcher.Dispatch(_messageMock.Object));
        Assert.Equal(_messageMock.Object, ex.FailedMessage);
    }

    [Fact]
    public void TestNoExceptionEnhancement()
    {
        _dispatcher.AddHandler(_handlerMock.Object);
        IMessage doNotReplaceThisMessage = IntegrationMessageBuilder.WithPayload("x").Build();
        _handlerMock.Setup(h => h.HandleMessage(_messageMock.Object)).Throws(new MessagingException(doNotReplaceThisMessage, "Mock Exception"));
        var ex = Assert.Throws<MessagingException>(() => _dispatcher.Dispatch(_messageMock.Object));
        Assert.Equal("Mock Exception", ex.Message);
        Assert.Equal(doNotReplaceThisMessage, ex.FailedMessage);
    }

    [Fact]
    public void TestFailOver()
    {
        var testException = new Exception("intentional");
        _handlerMock.Setup(h => h.HandleMessage(_messageMock.Object)).Throws(testException);

        _dispatcher.AddHandler(_handlerMock.Object);
        _dispatcher.AddHandler(_differentHandlerMock.Object);

        _dispatcher.Dispatch(_messageMock.Object);
        _handlerMock.Verify(h => h.HandleMessage(_messageMock.Object));
        _differentHandlerMock.Verify(h => h.HandleMessage(_messageMock.Object));
    }
}
