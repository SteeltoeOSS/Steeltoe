// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Moq;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[Trait("Category", "Integration")]
public class RabbitTemplateSimpleRoutingConnectionFactoryTest
{
    protected const string ConnectionFactoryName1 = "foo";
    protected const string ConnectionFactoryName2 = "bar";

    protected RabbitTemplate RoutingTemplate { get; }

    protected Mock<IConnectionFactory> ConnectionFactory1 { get; }
    protected Mock<IConnectionFactory> ConnectionFactory2 { get; }
    protected Mock<IConnectionFactory> DefaultConnectionFactory { get; }

    public RabbitTemplateSimpleRoutingConnectionFactoryTest()
    {
        RoutingTemplate = new RabbitTemplate();

        var routingConnFactory = new SimpleRoutingConnectionFactory();

        ConnectionFactory1 = new Mock<IConnectionFactory>();
        ConnectionFactory2 = new Mock<IConnectionFactory>();
        DefaultConnectionFactory = new Mock<IConnectionFactory>();
        routingConnFactory.AddTargetConnectionFactory(ConnectionFactoryName1, ConnectionFactory1.Object);
        routingConnFactory.AddTargetConnectionFactory(ConnectionFactoryName2, ConnectionFactory2.Object);

        RoutingTemplate.ConnectionFactory = routingConnFactory;
    }

    [Fact]
    public void ConvertSendAndReceiveShouldBindToRoutingConnectionFactoriesWithSimpleResourceHolder()
    {
        static Mock<RC.IModel> SetupMocks(Mock<IConnectionFactory> cf)
        {
            var connection = new Mock<IConnection>();
            var channel = new Mock<RC.IModel>();
            cf.Setup(f => f.CreateConnection()).Returns(connection.Object);
            connection.Setup(c => c.CreateChannel(false)).Returns(channel.Object);
            connection.Setup(c => c.IsOpen).Returns(true);
            channel.Setup(c => c.IsOpen).Returns(true);
            channel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            channel.Setup(c => c.QueueDeclarePassive(Address.AmqRabbitMQReplyTo)).Returns(() => new RC.QueueDeclareOk(Address.AmqRabbitMQReplyTo, 0, 0));
            return channel;
        }

        Mock<RC.IModel> channel1 = SetupMocks(ConnectionFactory1);
        Mock<RC.IModel> channel2 = SetupMocks(ConnectionFactory2);

        // act(a): send message using connection factory 1
        SimpleResourceHolder.Bind(RoutingTemplate.ConnectionFactory, ConnectionFactoryName1);
        RoutingTemplate.ConvertSendAndReceive<string>("exchFoo", "rkFoo", "msgFoo");
        SimpleResourceHolder.UnbindIfPossible(RoutingTemplate.ConnectionFactory);

        // act(b): send message using connection factory 2
        SimpleResourceHolder.Bind(RoutingTemplate.ConnectionFactory, ConnectionFactoryName2);
        RoutingTemplate.ConvertSendAndReceive<string>("exchBar", "rkBar", "msgBar");
        SimpleResourceHolder.UnbindIfPossible(RoutingTemplate.ConnectionFactory);

        // assert: both connection factories should be used
        ConnectionFactory1.Verify(cf => cf.CreateConnection(), Times.AtLeastOnce);
        channel1.Verify(c => c.BasicPublish("exchFoo", "rkFoo", It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));

        ConnectionFactory2.Verify(cf => cf.CreateConnection(), Times.AtLeastOnce);
        channel2.Verify(c => c.BasicPublish("exchBar", "rkBar", It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));
    }
}
