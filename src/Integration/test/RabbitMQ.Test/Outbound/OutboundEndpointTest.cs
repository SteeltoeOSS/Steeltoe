// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System.Text;
using Xunit;

namespace Steeltoe.Integration.Rabbit.Outbound
{
    public class OutboundEndpointTest
    {
        [Fact]
        public void TestDelay()
        {
            var config = new ConfigurationBuilder().Build();
            var services = new ServiceCollection().BuildServiceProvider();
            var context = new GenericApplicationContext(services, config);

            var connectionFactory = new Mock<IConnectionFactory>();
            var ampqTemplate = new TestRabbitTemplate
            {
                ConnectionFactory = connectionFactory.Object
            };
            var endpoint = new RabbitOutboundEndpoint(context, ampqTemplate, null)
            {
                ExchangeName = "foo",
                RoutingKey = "bar"
            };
            endpoint.SetDelayExpressionString("42");
            endpoint.Initialize();

            endpoint.HandleMessage(Message.Create("foo"));
            Assert.NotNull(ampqTemplate.SendMessage);
            Assert.Equal("foo", ampqTemplate.ExchangeName);
            Assert.Equal("bar", ampqTemplate.RoutingKey);
            Assert.Equal(42, ampqTemplate.SendMessage.Headers.Delay().Value);

            endpoint.ExpectReply = true;
            endpoint.OutputChannel = new NullChannel();
            endpoint.HandleMessage(Message.Create("foo"));
            Assert.NotNull(ampqTemplate.SendAndReceiveMessage);
            Assert.Equal("foo", ampqTemplate.ExchangeName);
            Assert.Equal("bar", ampqTemplate.RoutingKey);
            Assert.Equal(42, ampqTemplate.SendAndReceiveMessage.Headers.Delay().Value);

            endpoint.SetDelay(23);
            endpoint.RoutingKey = "baz";
            endpoint.Initialize();
            endpoint.HandleMessage(Message.Create("foo"));
            Assert.NotNull(ampqTemplate.SendAndReceiveMessage);
            Assert.Equal("foo", ampqTemplate.ExchangeName);
            Assert.Equal("baz", ampqTemplate.RoutingKey);
            Assert.Equal(23, ampqTemplate.SendAndReceiveMessage.Headers.Delay().Value);
        }

        public class TestRabbitTemplate : RabbitTemplate
        {
            public IMessage SendMessage { get; set; }

            public IMessage SendAndReceiveMessage { get; set; }

            public string ExchangeName { get; set; }

            public override void Send(string exchange, string routingKey, IMessage message, CorrelationData correlationData)
            {
                ExchangeName = exchange;
                RoutingKey = routingKey;
                SendMessage = message;
            }

            public override IMessage SendAndReceive(string exchange, string routingKey, IMessage message, CorrelationData correlationData)
            {
                SendAndReceiveMessage = message;
                ExchangeName = exchange;
                RoutingKey = routingKey;
                return Message.Create(Encoding.UTF8.GetBytes("foo"));
            }
        }
    }
}
