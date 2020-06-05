// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Endpoint.Test
{
    public class CorrelationIdTest
    {
        private readonly IServiceProvider provider;

        public CorrelationIdTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.AddSingleton<IDestinationResolver<IMessageChannel>, DefaultMessageChannelDestinationResolver>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            services.AddSingleton<IMessageChannel>((p) => new DirectChannel(p, "errorChannel"));
            provider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task TestCorrelationIdPassedIfAvailable()
        {
            object correlationId = "123-ABC";
            var message = Support.MessageBuilder.WithPayload("test").SetCorrelationId(correlationId).Build();
            var inputChannel = new DirectChannel(provider);
            var outputChannel = new QueueChannel(provider, 1);
            var serviceActivator = new ServiceActivatingHandler(provider, new TestBeanUpperCase());
            serviceActivator.OutputChannel = outputChannel;
            var endpoint = new EventDrivenConsumerEndpoint(provider, inputChannel, serviceActivator);
            await endpoint.Start();
            Assert.True(inputChannel.Send(message));
            var reply = outputChannel.Receive(0);
            var accessor = new IntegrationMessageHeaderAccessor(reply);
            Assert.Equal(correlationId, accessor.GetCorrelationId());
        }

        [Fact]
        public async Task TestCorrelationIdCopiedFromMessageCorrelationIdIfAvailable()
        {
            object correlationId = "correlationId";
            var message = Support.MessageBuilder.WithPayload("test").SetCorrelationId(correlationId).Build();
            var inputChannel = new DirectChannel(provider);
            var outputChannel = new QueueChannel(provider, 1);
            var serviceActivator = new ServiceActivatingHandler(provider, new TestBeanUpperCase());
            serviceActivator.OutputChannel = outputChannel;
            var endpoint = new EventDrivenConsumerEndpoint(provider, inputChannel, serviceActivator);
            await endpoint.Start();
            Assert.True(inputChannel.Send(message));
            var reply = outputChannel.Receive(0);
            var accessor1 = new IntegrationMessageHeaderAccessor(reply);
            var accessor2 = new IntegrationMessageHeaderAccessor(message);
            Assert.Equal(accessor2.GetCorrelationId(), accessor1.GetCorrelationId());
        }

        [Fact]
        public async Task TestCorrelationNotPassedFromRequestHeaderIfAlreadySetByHandler()
        {
            object correlationId = "123-ABC";
            var message = Support.MessageBuilder.WithPayload("test").SetCorrelationId(correlationId).Build();
            var inputChannel = new DirectChannel(provider);
            var outputChannel = new QueueChannel(provider, 1);
            var serviceActivator = new ServiceActivatingHandler(provider, new TestBeanCreateMessage());
            serviceActivator.OutputChannel = outputChannel;
            var endpoint = new EventDrivenConsumerEndpoint(provider, inputChannel, serviceActivator);
            await endpoint.Start();
            Assert.True(inputChannel.Send(message));
            var reply = outputChannel.Receive(0);
            var accessor = new IntegrationMessageHeaderAccessor(reply);
            Assert.Equal("456-XYZ", accessor.GetCorrelationId());
        }

        [Fact]
        public async Task TestCorrelationNotCopiedFromRequestMessgeIdIfAlreadySetByHandler()
        {
            IMessage message = new GenericMessage("test");
            var inputChannel = new DirectChannel(provider);
            var outputChannel = new QueueChannel(provider, 1);
            var serviceActivator = new ServiceActivatingHandler(provider, new TestBeanCreateMessage());
            serviceActivator.OutputChannel = outputChannel;
            var endpoint = new EventDrivenConsumerEndpoint(provider, inputChannel, serviceActivator);
            await endpoint.Start();
            Assert.True(inputChannel.Send(message));
            var reply = outputChannel.Receive(0);
            var accessor = new IntegrationMessageHeaderAccessor(reply);
            Assert.Equal("456-XYZ", accessor.GetCorrelationId());
        }

        private class TestBeanUpperCase : IMessageProcessor
        {
            public object ProcessMessage(IMessage message)
            {
                var str = message.Payload as string;
                return str.ToUpper();
            }
        }

        private class TestBeanCreateMessage : IMessageProcessor
        {
            public object ProcessMessage(IMessage message)
            {
                var str = message.Payload as string;
                return Support.MessageBuilder.WithPayload(str).SetCorrelationId("456-XYZ").Build();
            }
        }
    }
}
