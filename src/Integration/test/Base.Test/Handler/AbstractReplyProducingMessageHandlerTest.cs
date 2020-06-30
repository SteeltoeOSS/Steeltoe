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
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Integration.Handler.Test
{
    public class AbstractReplyProducingMessageHandlerTest
    {
        private readonly Mock<IMessageChannel> mockChannel;
        private readonly TestAbstractReplyProducingMessageHandler handler;
        private readonly IMessage message;
        private readonly IServiceProvider provider;

        public AbstractReplyProducingMessageHandlerTest()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IApplicationContext, GenericApplicationContext>();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();
            handler = new TestAbstractReplyProducingMessageHandler(provider.GetService<IApplicationContext>());
            mockChannel = new Mock<IMessageChannel>();
            message = Integration.Support.IntegrationMessageBuilder.WithPayload("test").Build();
        }

        [Fact]
        public void ErrorMessageShouldContainChannelName()
        {
            handler.OutputChannel = mockChannel.Object;
            mockChannel.Setup((c) => c.Send(message)).Returns(false);
            mockChannel.Setup((c) => c.ToString()).Returns("testChannel");
            try
            {
                handler.HandleMessage(message);
                throw new Exception("Expected a MessagingException");
            }
            catch (MessagingException e)
            {
                Assert.Contains("AbstractReplyProducingMessageHandler", e.Message);
            }
        }

        [Fact]
        public void TestNotPropagate()
        {
            handler.ReturnValue = Message.Create("world", new Dictionary<string, object>() { { "bar", "RAB" } });
            Assert.Empty(handler.NotPropagatedHeaders);
            handler.NotPropagatedHeaders = new List<string>() { "f*", "*r" };
            handler.OutputChannel = mockChannel.Object;
            IMessage captor = null;
            mockChannel.Setup((c) => c.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Returns(true).Callback<IMessage, int>((m, t) => captor = m);
            mockChannel.Setup((c) => c.ToString()).Returns("testChannel");

            handler.HandleMessage(Integration.Support.IntegrationMessageBuilder.WithPayload("hello")
                    .SetHeader("foo", "FOO")
                    .SetHeader("bar", "BAR")
                    .SetHeader("baz", "BAZ")
                    .Build());

            Assert.NotNull(captor);
            Assert.Null(captor.Headers.Get<string>("foo"));
            Assert.Equal("RAB", captor.Headers.Get<string>("bar"));
            Assert.Equal("BAZ", captor.Headers.Get<string>("baz"));
        }

        [Fact]
        public void TestNotPropagateAddWhenNonExist()
        {
            handler.ReturnValue = Message.Create("world", new Dictionary<string, object>() { { "bar", "RAB" } });
            Assert.Empty(handler.NotPropagatedHeaders);
            handler.AddNotPropagatedHeaders("boom");
            handler.OutputChannel = mockChannel.Object;
            IMessage captor = null;
            mockChannel.Setup((c) => c.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Returns(true).Callback<IMessage, int>((m, t) => captor = m);
            mockChannel.Setup((c) => c.ToString()).Returns("testChannel");

            handler.HandleMessage(Integration.Support.IntegrationMessageBuilder.WithPayload("hello")
                    .SetHeader("boom", "FOO")
                    .SetHeader("bar", "BAR")
                    .SetHeader("baz", "BAZ")
                    .Build());

            Assert.NotNull(captor);
            Assert.Null(captor.Headers.Get<string>("boom"));
            Assert.Equal("RAB", captor.Headers.Get<string>("bar"));
            Assert.Equal("BAZ", captor.Headers.Get<string>("baz"));
        }

        [Fact]
        public void TestNotPropagateAdd()
        {
            handler.ReturnValue = Message.Create("world", new Dictionary<string, object>() { { "bar", "RAB" } });
            Assert.Empty(handler.NotPropagatedHeaders);
            handler.NotPropagatedHeaders = new List<string>() { "foo" };
            handler.AddNotPropagatedHeaders("b*r");
            handler.OutputChannel = mockChannel.Object;
            IMessage captor = null;
            mockChannel.Setup((c) => c.Send(It.IsAny<IMessage>(), It.IsAny<int>())).Returns(true).Callback<IMessage, int>((m, t) => captor = m);
            mockChannel.Setup((c) => c.ToString()).Returns("testChannel");

            handler.HandleMessage(Integration.Support.IntegrationMessageBuilder.WithPayload("hello")
                    .SetHeader("foo", "FOO")
                    .SetHeader("bar", "BAR")
                    .SetHeader("baz", "BAZ")
                    .Build());

            Assert.NotNull(captor);
            Assert.Null(captor.Headers.Get<string>("foo"));
            Assert.Equal("RAB", captor.Headers.Get<string>("bar"));
            Assert.Equal("BAZ", captor.Headers.Get<string>("baz"));
        }

        private class TestAbstractReplyProducingMessageHandler : AbstractReplyProducingMessageHandler
        {
            public object ReturnValue;

            public TestAbstractReplyProducingMessageHandler(IApplicationContext context)
                : base(context)
            {
            }

            protected override object HandleRequestMessage(IMessage requestMessage)
            {
                if (ReturnValue != null)
                {
                    return ReturnValue;
                }

                throw new NotImplementedException();
            }
        }
    }
}
