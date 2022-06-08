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
    protected const string CONNECTION_FACTORY_1 = "foo";
    protected const string CONNECTION_FACTORY_2 = "bar";

    protected RabbitTemplate routingTemplate;

    protected Mock<IConnectionFactory> cf1;
    protected Mock<IConnectionFactory> cf2;
    protected Mock<IConnectionFactory> defaultCF;

    public RabbitTemplateSimpleRoutingConnectionFactoryTest()
    {
        routingTemplate = new RabbitTemplate();

        var routingConnFactory = new SimpleRoutingConnectionFactory();

        cf1 = new Mock<IConnectionFactory>();
        cf2 = new Mock<IConnectionFactory>();
        defaultCF = new Mock<IConnectionFactory>();
        routingConnFactory.AddTargetConnectionFactory(CONNECTION_FACTORY_1, cf1.Object);
        routingConnFactory.AddTargetConnectionFactory(CONNECTION_FACTORY_2, cf2.Object);

        routingTemplate.ConnectionFactory = routingConnFactory;
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
            channel.Setup(c => c.QueueDeclarePassive(Address.AMQ_RABBITMQ_REPLY_TO))
                .Returns(() => new RC.QueueDeclareOk(Address.AMQ_RABBITMQ_REPLY_TO, 0, 0));
            return channel;
        }

        var channel1 = SetupMocks(cf1);
        var channel2 = SetupMocks(cf2);

        // act(a): send message using connection factory 1
        SimpleResourceHolder.Bind(routingTemplate.ConnectionFactory, CONNECTION_FACTORY_1);
        routingTemplate.ConvertSendAndReceive<string>("exchFoo", "rkFoo", "msgFoo");
        SimpleResourceHolder.UnbindIfPossible(routingTemplate.ConnectionFactory);

        // act(b): send message using connection factory 2
        SimpleResourceHolder.Bind(routingTemplate.ConnectionFactory, CONNECTION_FACTORY_2);
        routingTemplate.ConvertSendAndReceive<string>("exchBar", "rkBar", "msgBar");
        SimpleResourceHolder.UnbindIfPossible(routingTemplate.ConnectionFactory);

        // assert: both connection factories should be used
        cf1.Verify(cf => cf.CreateConnection(), Times.AtLeastOnce);
        channel1.Verify(c => c.BasicPublish("exchFoo", "rkFoo", It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));

        cf2.Verify(cf => cf.CreateConnection(), Times.AtLeastOnce);
        channel2.Verify(c => c.BasicPublish("exchBar", "rkBar", It.IsAny<bool>(), It.IsAny<RC.IBasicProperties>(), It.IsAny<byte[]>()));
    }
}
