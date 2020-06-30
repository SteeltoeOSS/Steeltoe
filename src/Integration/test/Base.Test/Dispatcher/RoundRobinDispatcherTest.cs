// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
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
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IApplicationContext, GenericApplicationContext>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            provider = services.BuildServiceProvider();
            dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
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
            var dontReplaceThisMessage = IntegrationMessageBuilder.WithPayload("x").Build();
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
