// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Moq;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MongoDb;
using Xunit;

namespace Steeltoe.Connectors.Test.MongoDb;

public sealed class MongoDbHealthContributorTest
{
    [Fact]
    public async Task Not_Connected_Returns_Down_Status()
    {
        var settings = new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost"),
            ServerSelectionTimeout = TimeSpan.FromMilliseconds(1)
        };

        var mongoClient = new MongoClient(settings);

        var healthContributor = new MongoDbHealthContributor(mongoClient, "localhost", NullLogger<MongoDbHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("MongoDB health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("TimeoutException: A timeout occurred after 1ms selecting ");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var mongoClientMock = new Mock<IMongoClient>();

        var healthContributor = new MongoDbHealthContributor(mongoClientMock.Object, "localhost", NullLogger<MongoDbHealthContributor>.Instance)
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
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        var mongoClientMock = new Mock<IMongoClient>();

        mongoClientMock.Setup(client => client.ListDatabaseNamesAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null!;
        });

        var healthContributor = new MongoDbHealthContributor(mongoClientMock.Object, "localhost", NullLogger<MongoDbHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        source.Cancel();

        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }

    [Fact(Skip = "Integration test - Requires local MongoDb server")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        var settings = new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost"),
            ServerSelectionTimeout = TimeSpan.FromSeconds(5)
        };

        var mongoClient = new MongoClient(settings);

        var healthContributor = new MongoDbHealthContributor(mongoClient, "localhost", NullLogger<MongoDbHealthContributor>.Instance)
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
    }
}
