// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using System;
using Xunit;

namespace Steeltoe.Integration.Dispatcher.Test
{
    public class RoundRobinDispatcherTest
    {
        private readonly UnicastingDispatcher dispatcher;

        private readonly Mock<IMessage> messageMock = new Mock<IMessage>();

        private readonly Mock<IMessageHandler> handlerMock = new Mock<IMessageHandler>();

        private readonly Mock<IMessageHandler> differentHandlerMock = new Mock<IMessageHandler>();

        private readonly IServiceProvider provider;

        public RoundRobinDispatcherTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            provider = services.BuildServiceProvider();
            dispatcher = new UnicastingDispatcher(provider);
            dispatcher.LoadBalancingStrategy = new RoundRobinLoadBalancingStrategy();
        }

        [Fact]
        public void DispatchMessageWithSingleHandler()
        {
            dispatcher.AddHandler(handlerMock.Object);
            dispatcher.Dispatch(messageMock.Object);
            handlerMock.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void DifferentHandlerInvokedOnSecondMessage()
        {
            dispatcher.AddHandler(handlerMock.Object);
            dispatcher.AddHandler(differentHandlerMock.Object);
            dispatcher.Dispatch(messageMock.Object);
            dispatcher.Dispatch(messageMock.Object);
            handlerMock.Verify((h) => h.HandleMessage(messageMock.Object));
            differentHandlerMock.Verify((h) => h.HandleMessage(messageMock.Object));
        }

        [Fact]
        public void MultipleCyclesThroughHandlers()
        {
            dispatcher.AddHandler(handlerMock.Object);
            dispatcher.AddHandler(differentHandlerMock.Object);
            for (var i = 0; i < 7; i++)
            {
                dispatcher.Dispatch(messageMock.Object);
            }

            handlerMock.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(4));
            differentHandlerMock.Verify((h) => h.HandleMessage(messageMock.Object), Times.Exactly(3));
        }

        [Fact]
        public void CurrentHandlerIndexOverFlow()
        {
            dispatcher.AddHandler(handlerMock.Object);
            dispatcher.AddHandler(differentHandlerMock.Object);
            var balancer = dispatcher.LoadBalancingStrategy as RoundRobinLoadBalancingStrategy;
            balancer.CurrentHandlerIndex = int.MaxValue - 5;

            for (var i = 0; i < 40; i++)
            {
                dispatcher.Dispatch(messageMock.Object);
            }

            handlerMock.Verify((h) => h.HandleMessage(messageMock.Object), Times.AtLeast(18));
            differentHandlerMock.Verify((h) => h.HandleMessage(messageMock.Object), Times.AtLeast(18));
        }

        [Fact]
        public void TestExceptionEnhancement()
        {
            dispatcher.AddHandler(handlerMock.Object);
            handlerMock.Setup((h) => h.HandleMessage(messageMock.Object)).Throws(new MessagingException("Mock Exception"));
            var ex = Assert.Throws<MessageDeliveryException>(() => dispatcher.Dispatch(messageMock.Object));
            Assert.Equal(messageMock.Object, ex.FailedMessage);
        }

        [Fact]
        public void TestNoExceptionEnhancement()
        {
            dispatcher.AddHandler(handlerMock.Object);
            var dontReplaceThisMessage = MessageBuilder.WithPayload("x").Build();
            handlerMock.Setup((h) => h.HandleMessage(messageMock.Object)).Throws(new MessagingException(dontReplaceThisMessage, "Mock Exception"));
            var ex = Assert.Throws<MessagingException>(() => dispatcher.Dispatch(messageMock.Object));
            Assert.Equal("Mock Exception", ex.Message);
            Assert.Equal(dontReplaceThisMessage, ex.FailedMessage);
        }

        [Fact]
        public void TestFailOver()
        {
            var testException = new Exception("intentional");
            handlerMock.Setup((h) => h.HandleMessage(messageMock.Object)).Throws(testException);

            dispatcher.AddHandler(handlerMock.Object);
            dispatcher.AddHandler(differentHandlerMock.Object);

            dispatcher.Dispatch(messageMock.Object);
            handlerMock.Verify((h) => h.HandleMessage(messageMock.Object));
            differentHandlerMock.Verify((h) => h.HandleMessage(messageMock.Object));
        }
    }
}
