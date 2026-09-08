// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.RabbitMQ;

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

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("RabbitMQ health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().Contain("error", "BrokerUnreachableException: None of the specified endpoints were reachable");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);

        connection.ServerProperties.Returns(new Dictionary<string, object?>
        {
            ["version"] = "1.2.3"u8.ToArray()
        });

        var connectionFactory = Substitute.For<IConnectionFactory>();
        connectionFactory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(connection));

        using var healthContributor = new RabbitMQHealthContributor(connectionFactory, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
        result.Details.Should().Contain("version", "1.2.3");
    }

    [Fact]
    public async Task Lost_Connection_Returns_Down_Status()
    {
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);

        connection.ServerProperties.Returns(new Dictionary<string, object?>
        {
            ["version"] = "1.2.3"u8.ToArray()
        });

        var connectionFactory = Substitute.For<IConnectionFactory>();
        connectionFactory.CreateConnectionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(connection));

        using var healthContributor = new RabbitMQHealthContributor(connectionFactory, "localhost", NullLogger<RabbitMQHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        // Ensure initial connection is obtained.
        _ = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        connection.IsOpen.Returns(false);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("RabbitMQ health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().Contain("error", "IOException: RabbitMQ connection is closed!");
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        var connectionFactory = Substitute.For<IConnectionFactory>();
        using var healthContributor = new RabbitMQHealthContributor(connectionFactory, "localhost", NullLogger<RabbitMQHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);
        // ReSharper restore AccessToDisposedClosure

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

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
        result.Details.Should().ContainKey("version");
    }
}
