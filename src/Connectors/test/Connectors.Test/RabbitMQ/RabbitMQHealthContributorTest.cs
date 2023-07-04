// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.RabbitMQ;
using Xunit;

namespace Steeltoe.Connectors.Test.RabbitMQ;

public sealed class RabbitMQHealthContributorTest
{
    [Fact]
    public void Not_Connected_Returns_Down_Status()
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri("amqp://si_username:si_password@localhost:5672/si_vhost"),
            RequestedConnectionTimeout = 1.Milliseconds()
        };

        using var healthContributor = new RabbitMQHealthContributor(connectionFactory, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("RabbitMQ health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "BrokerUnreachableException: None of the specified endpoints were reachable");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact]
    public void Is_Connected_Returns_Up_Status()
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.Setup(connection => connection.IsOpen).Returns(true);

        connectionMock.Setup(connection => connection.ServerProperties).Returns(new Dictionary<string, object>
        {
            { "version", Encoding.UTF8.GetBytes("1.2.3") }
        });

        var connectionFactoryMock = new Mock<IConnectionFactory>();
        connectionFactoryMock.Setup(factory => factory.CreateConnection()).Returns(connectionMock.Object);

        using var healthContributor = new RabbitMQHealthContributor(connectionFactoryMock.Object, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
        status.Details.Should().Contain("version", "1.2.3");
    }

    [Fact]
    public void Lost_Connection_Returns_Down_Status()
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.Setup(connection => connection.IsOpen).Returns(true);

        connectionMock.Setup(connection => connection.ServerProperties).Returns(new Dictionary<string, object>
        {
            { "version", Encoding.UTF8.GetBytes("1.2.3") }
        });

        var connectionFactoryMock = new Mock<IConnectionFactory>();
        connectionFactoryMock.Setup(factory => factory.CreateConnection()).Returns(connectionMock.Object);

        using var healthContributor = new RabbitMQHealthContributor(connectionFactoryMock.Object, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        // Ensure initial connection is obtained.
        _ = healthContributor.Health();

        connectionMock.Setup(connection => connection.IsOpen).Returns(false);

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("RabbitMQ health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "ConnectorException: RabbitMQ connection is closed!");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact(Skip = "Integration test - Requires local RabbitMQ server")]
    public void Integration_Is_Connected_Returns_Up_Status()
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri("amqp://localhost:5672")
        };

        using var healthContributor = new RabbitMQHealthContributor(connectionFactory, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult status = healthContributor.Health();

        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
        status.Details.Should().ContainKey("version");
    }
}
