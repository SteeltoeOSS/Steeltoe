// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
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
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IApplicationContext, GenericApplicationContext>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();
            handler = new BridgeHandler(provider.GetService<IApplicationContext>());
        }

        [Fact]
        public void SimpleBridge()
        {
            var outputChannel = new QueueChannel(provider.GetService<IApplicationContext>());
            handler.OutputChannel = outputChannel;
            var request = Message.Create("test");
            handler.HandleMessage(request);
            var reply = outputChannel.Receive(0);
            Assert.NotNull(reply);
            Assert.Equal(request.Payload, reply.Payload);
            Assert.Equal(request.Headers, reply.Headers);
        }

        [Fact]
        public void MissingOutputChannelVerifiedAtRuntime()
        {
            var request = Message.Create("test");
            var ex = Assert.Throws<MessageHandlingException>(() => handler.HandleMessage(request));
            Assert.IsType<DestinationResolutionException>(ex.InnerException);
        }

        [Fact]
        public void MissingOutputChannelAllowedForReplyChannelMessages()
        {
            var replyChannel = new QueueChannel(provider.GetService<IApplicationContext>());
            var request = IntegrationMessageBuilder.WithPayload("tst").SetReplyChannel(replyChannel).Build();
            handler.HandleMessage(request);
            var reply = replyChannel.Receive();
            Assert.NotNull(reply);
            Assert.Equal(request.Payload, reply.Payload);
            Assert.Equal(request.Headers, reply.Headers);
        }
    }
}
