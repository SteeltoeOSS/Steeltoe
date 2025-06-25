// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Connectors.Test.Redis;

public sealed class RedisHealthContributorTest
{
    [Fact]
    public async Task Not_Connected_Returns_Down_Status()
    {
        var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();

        connectionMultiplexerMock.Setup(connectionMultiplexer => connectionMultiplexer.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "It was not possible to connect ..."));

        using var healthContributor = new RedisHealthContributor("localhost", NullLogger<RedisHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        healthContributor.SetConnectionMultiplexer(connectionMultiplexerMock.Object);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("Redis health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("RedisConnectionException: It was not possible to connect ");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var databaseMock = new Mock<IDatabase>();
        databaseMock.Setup(database => database.PingAsync(It.IsAny<CommandFlags>())).Returns(Task.FromResult(50.Milliseconds()));

        var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();

        connectionMultiplexerMock.Setup(connectionMultiplexer => connectionMultiplexer.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(databaseMock.Object);

        using var healthContributor = new RedisHealthContributor("localhost", NullLogger<RedisHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        healthContributor.SetConnectionMultiplexer(connectionMultiplexerMock.Object);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
        result.Details.Should().Contain("ping", 50D);
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        using var healthContributor = new RedisHealthContributor("localhost", NullLogger<RedisHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }

    [Fact(Skip = "Integration test - Requires local Redis server")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        const string connectionString = "localhost";

        using var healthContributor = new RedisHealthContributor(connectionString, NullLogger<RedisHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
        result.Details.Should().ContainKey("ping");
    }
}
