// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Xunit;

namespace Steeltoe.Integration.Rabbit.Outbound;

public class OutboundEndpointTest
{
    [Fact]
    public void TestDelay()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var context = new GenericApplicationContext(services, configurationRoot);

        var connectionFactory = new Mock<IConnectionFactory>();

        var rabbitTemplate = new TestRabbitTemplate
        {
            ConnectionFactory = connectionFactory.Object
        };

        var endpoint = new RabbitOutboundEndpoint(context, rabbitTemplate, null)
        {
            ExchangeName = "foo",
            RoutingKey = "bar"
        };

        endpoint.SetDelayExpressionString("42");
        endpoint.Initialize();

        endpoint.HandleMessage(Message.Create("foo"));
        Assert.NotNull(rabbitTemplate.SendMessage);
        Assert.Equal("foo", rabbitTemplate.ExchangeName);
        Assert.Equal("bar", rabbitTemplate.RoutingKey);
        Assert.Equal(42, rabbitTemplate.SendMessage.Headers.Delay().Value);

        endpoint.ExpectReply = true;
        endpoint.OutputChannel = new NullChannel();
        endpoint.HandleMessage(Message.Create("foo"));
        Assert.NotNull(rabbitTemplate.SendAndReceiveMessage);
        Assert.Equal("foo", rabbitTemplate.ExchangeName);
        Assert.Equal("bar", rabbitTemplate.RoutingKey);
        Assert.Equal(42, rabbitTemplate.SendAndReceiveMessage.Headers.Delay().Value);

        endpoint.SetDelay(23);
        endpoint.RoutingKey = "baz";
        endpoint.Initialize();
        endpoint.HandleMessage(Message.Create("foo"));
        Assert.NotNull(rabbitTemplate.SendAndReceiveMessage);
        Assert.Equal("foo", rabbitTemplate.ExchangeName);
        Assert.Equal("baz", rabbitTemplate.RoutingKey);
        Assert.Equal(23, rabbitTemplate.SendAndReceiveMessage.Headers.Delay().Value);
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
