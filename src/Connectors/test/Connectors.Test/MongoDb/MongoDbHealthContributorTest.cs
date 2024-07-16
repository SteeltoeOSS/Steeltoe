// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Moq;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MongoDb;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.Test.MongoDb;

public sealed class MongoDbHealthContributorTest
{
    [Fact]
    public async Task Not_Connected_Returns_Down_Status()
    {
        const string serviceName = "Example";
        const string connectionString = "mongodb://localhost:27017";

        var mongoClient = new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost"),
            ServerSelectionTimeout = TimeSpan.FromMilliseconds(1)
        });

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, mongoClient);

        var healthContributor = new MongoDbHealthContributor(serviceName, serviceProvider, MongoDbPackageResolver.Default,
            NullLogger<MongoDbHealthContributor>.Instance);

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
        const string serviceName = "Example";
        const string connectionString = "mongodb://localhost:27017";

        var mongoClientMock = new Mock<IMongoClient>();

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, mongoClientMock.Object);

        var healthContributor = new MongoDbHealthContributor(serviceName, serviceProvider, MongoDbPackageResolver.Default,
            NullLogger<MongoDbHealthContributor>.Instance);

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
        const string serviceName = "Example";
        const string connectionString = "mongodb://localhost:27017";

        var mongoClientMock = new Mock<IMongoClient>();

        mongoClientMock.Setup(client => client.ListDatabaseNamesAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null!;
        });

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, mongoClientMock.Object);

        var healthContributor = new MongoDbHealthContributor(serviceName, serviceProvider, MongoDbPackageResolver.Default,
            NullLogger<MongoDbHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }

    [Fact(Skip = "Integration test - Requires local MongoDb server")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        const string serviceName = "Example";
        const string connectionString = "mongodb://localhost:27017";

        var mongoClient = new MongoClient(new MongoClientSettings
        {
            Server = new MongoServerAddress("localhost"),
            ServerSelectionTimeout = TimeSpan.FromSeconds(5)
        });

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, mongoClient);

        var healthContributor = new MongoDbHealthContributor(serviceName, serviceProvider, MongoDbPackageResolver.Default,
            NullLogger<MongoDbHealthContributor>.Instance);

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().NotContainKey("error");
        status.Details.Should().Contain("status", "UP");
    }

    private ServiceProvider CreateServiceProvider(string serviceName, string connectionString, IMongoClient mongoClient)
    {
        var serviceNames = (HashSet<string>) [serviceName];

        var services = new ServiceCollection();
        services.AddOptions<MongoDbOptions>(serviceName).Configure(dbOptions => dbOptions.ConnectionString = connectionString);
        services.AddSingleton(provider => new ConnectorFactory<MongoDbOptions, IMongoClient>(provider, serviceNames, (_, _) => mongoClient, true));

        return services.BuildServiceProvider();
    }
}
