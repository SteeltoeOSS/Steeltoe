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
    public async Task Not_Connected_Returns_Down_Status()
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

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("RabbitMQ health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "BrokerUnreachableException: None of the specified endpoints were reachable");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
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

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
        status.Details.Should().Contain("version", "1.2.3");
    }

    [Fact]
    public async Task Lost_Connection_Returns_Down_Status()
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
        _ = await healthContributor.CheckHealthAsync(CancellationToken.None);

        connectionMock.Setup(connection => connection.IsOpen).Returns(false);

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("RabbitMQ health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "IOException: RabbitMQ connection is closed!");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        var connectionFactoryMock = new Mock<IConnectionFactory>();
        using var healthContributor = new RabbitMQHealthContributor(connectionFactoryMock.Object, "localhost", NullLogger<RabbitMQHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }

    [Fact(Skip = "Integration test - Requires local RabbitMQ server")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri("amqp://localhost:5672")
        };

        using var healthContributor = new RabbitMQHealthContributor(connectionFactory, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
        status.Details.Should().ContainKey("version");
    }
}
