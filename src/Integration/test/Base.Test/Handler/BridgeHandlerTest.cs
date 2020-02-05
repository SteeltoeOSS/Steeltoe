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
using Steeltoe.Messaging.Support;
using System;
using Xunit;

namespace Steeltoe.Integration.Handler.Test
{
    public class BridgeHandlerTest
    {
        private readonly BridgeHandler handler;
        private readonly IServiceProvider provider;

        public BridgeHandlerTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();
            handler = new BridgeHandler(provider);
        }

        [Fact]
        public void SimpleBridge()
        {
            var outputChannel = new QueueChannel(provider);
            handler.OutputChannel = outputChannel;
            var request = new GenericMessage("test");
            handler.HandleMessage(request);
            var reply = outputChannel.Receive(0);
            Assert.NotNull(reply);
            Assert.Equal(request.Payload, reply.Payload);
            Assert.Equal(request.Headers, reply.Headers);
        }

        [Fact]
        public void MissingOutputChannelVerifiedAtRuntime()
        {
            var request = new GenericMessage("test");
            var ex = Assert.Throws<MessageHandlingException>(() => handler.HandleMessage(request));
            Assert.IsType<DestinationResolutionException>(ex.InnerException);
        }

        [Fact]
        public void MissingOutputChannelAllowedForReplyChannelMessages()
        {
            var replyChannel = new QueueChannel(provider);
            var request = Integration.Support.MessageBuilder.WithPayload("tst").SetReplyChannel(replyChannel).Build();
            handler.HandleMessage(request);
            var reply = replyChannel.Receive();
            Assert.NotNull(reply);
            Assert.Equal(request.Payload, reply.Payload);
            Assert.Equal(request.Headers, reply.Headers);
        }
    }
}
