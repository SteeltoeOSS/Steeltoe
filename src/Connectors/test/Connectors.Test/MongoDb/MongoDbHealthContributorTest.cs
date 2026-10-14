// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NSubstitute;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MongoDb;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.Test.MongoDb;

public sealed class MongoDbHealthContributorTest
{
    private const string ExampleServiceName = "Example";

    [Fact]
    public async Task Not_Connected_Returns_Down_Status()
    {
        using var mongoClient = new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost"),
            ServerSelectionTimeout = 1.Milliseconds()
        });

        var mongoClientShim = new MongoClientInterfaceShim(MongoDbPackageResolver.Default, mongoClient);

        var healthContributor = new MongoDbHealthContributor(ExampleServiceName, () => mongoClientShim, false, "localhost",
            NullLogger<MongoDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("MongoDB health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", ExampleServiceName);
        result.Details.Should().ContainKey("error").WhoseValue.As<string>().Should().StartWith("TimeoutException: A timeout occurred after");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoClientShim = new MongoClientInterfaceShim(MongoDbPackageResolver.Default, mongoClient);

        var healthContributor =
            new MongoDbHealthContributor(ExampleServiceName, () => mongoClientShim, true, "localhost", NullLogger<MongoDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", ExampleServiceName);
        result.Details.Should().NotContainKey("error");

        mongoClient.Received(1).Dispose();
    }

    [Fact]
    public async Task Does_not_dispose_client_it_does_not_own()
    {
        var mongoClient = Substitute.For<IMongoClient>();
        var mongoClientShim = new MongoClientInterfaceShim(MongoDbPackageResolver.Default, mongoClient);

        var healthContributor = new MongoDbHealthContributor(ExampleServiceName, () => mongoClientShim, false, "localhost",
            NullLogger<MongoDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);

        mongoClient.DidNotReceive().Dispose();
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        var mongoClient = Substitute.For<IMongoClient>();

        mongoClient.ListDatabaseNamesAsync(Arg.Any<CancellationToken>()).Returns(Task<IAsyncCursor<string>> (info) =>
        {
            info.Arg<CancellationToken>().ThrowIfCancellationRequested();
            return null!;
        });

        var mongoClientShim = new MongoClientInterfaceShim(MongoDbPackageResolver.Default, mongoClient);

        var healthContributor =
            new MongoDbHealthContributor(ExampleServiceName, () => mongoClientShim, true, "localhost", NullLogger<MongoDbHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();

        mongoClient.Received(1).Dispose();
    }

    [Fact(Skip = "Integration test - Requires local MongoDb server")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        var mongoClient = new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost"),
            ServerSelectionTimeout = 5.Seconds()
        });

        var mongoClientShim = new MongoClientInterfaceShim(MongoDbPackageResolver.Default, mongoClient);

        var healthContributor =
            new MongoDbHealthContributor(ExampleServiceName, () => mongoClientShim, true, "localhost", NullLogger<MongoDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", ExampleServiceName);
        result.Details.Should().NotContainKey("error");
    }
}
