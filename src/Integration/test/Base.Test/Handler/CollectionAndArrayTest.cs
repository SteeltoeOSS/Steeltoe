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

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Integration.Handler.Test
{
    public class CollectionAndArrayTest
    {
        private readonly TestAbstractReplyProducingMessageHandler handler;
        private readonly IServiceProvider provider;

        public CollectionAndArrayTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();
            handler = new TestAbstractReplyProducingMessageHandler(provider);
        }

        [Fact]
        public void ListWithRequestReplyHandler()
        {
            handler.ReturnValue = new List<string>() { "foo", "bar" };
            var channel = new QueueChannel(provider);
            var message = MessageBuilder.WithPayload("test").SetReplyChannel(channel).Build();
            handler.HandleMessage(message);
            var reply1 = channel.Receive(0);
            var reply2 = channel.Receive(0);
            Assert.NotNull(reply1);
            Assert.Null(reply2);
            Assert.IsType<List<string>>(reply1.Payload);
            Assert.Equal(2, ((List<string>)reply1.Payload).Count);
        }

        [Fact]
        public void SetWithRequestReplyHandler()
        {
            handler.ReturnValue = new HashSet<string>(new string[] { "foo", "bar" });
            var channel = new QueueChannel(provider);
            var message = MessageBuilder.WithPayload("test").SetReplyChannel(channel).Build();
            handler.HandleMessage(message);
            var reply1 = channel.Receive(0);
            var reply2 = channel.Receive(0);
            Assert.NotNull(reply1);
            Assert.Null(reply2);
            Assert.IsType<HashSet<string>>(reply1.Payload);
            Assert.Equal(2, ((HashSet<string>)reply1.Payload).Count);
        }

        [Fact]
        public void ArrayWithRequestReplyHandler()
        {
            handler.ReturnValue = new string[] { "foo", "bar" };
            var channel = new QueueChannel(provider);
            var message = MessageBuilder.WithPayload("test").SetReplyChannel(channel).Build();
            handler.HandleMessage(message);
            var reply1 = channel.Receive(0);
            var reply2 = channel.Receive(0);
            Assert.NotNull(reply1);
            Assert.Null(reply2);
            Assert.IsType<string[]>(reply1.Payload);
            Assert.Equal(2, ((string[])reply1.Payload).Length);
        }

        private class TestAbstractReplyProducingMessageHandler : AbstractReplyProducingMessageHandler
        {
            public object ReturnValue;

            public TestAbstractReplyProducingMessageHandler(IServiceProvider serviceProvider)
                : base(serviceProvider)
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
