// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Connectors.CosmosDb;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.Test.CosmosDb;

public sealed class CosmosDbHealthContributorTest
{
    [Fact]
    public async Task Not_Connected_Returns_Down_Status()
    {
        const string serviceName = "Example";
        const string connectionString = "AccountEndpoint=https://localhost:8081;AccountKey=IA==";

        DelegateToMockHttpClientHandler httpMessageHandler = new DelegateToMockHttpClientHandler().Setup(mockHttp =>
            mockHttp.When("*").Throw(new HttpRequestException("No connection could be made because ...")));

        var cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
        {
            HttpClientFactory = () => new HttpClient(httpMessageHandler)
        });

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, cosmosClient);

        using var healthContributor = new CosmosDbHealthContributor(serviceName, serviceProvider, CosmosDbPackageResolver.Default,
            NullLogger<CosmosDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("CosmosDB health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", serviceName);
        result.Details.Should().Contain("error", "HttpRequestException: No connection could be made because ...");
    }

    [Fact]
    public async Task Not_Connected_With_Timeout_Returns_Down_Status()
    {
        const string serviceName = "Example";
        const string connectionString = "AccountEndpoint=https://localhost:8081;AccountKey=IA==";

        var cosmosClient = new CosmosClient(connectionString);

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, cosmosClient);

        using var healthContributor = new CosmosDbHealthContributor(serviceName, serviceProvider, CosmosDbPackageResolver.Default,
            NullLogger<CosmosDbHealthContributor>.Instance);

        healthContributor.Timeout = 1.Milliseconds();

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Down);
        result.Description.Should().Be("CosmosDB health check failed");
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", serviceName);
        result.Details.Should().Contain("error", "TimeoutException: The operation has timed out.");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        const string serviceName = "Example";
        const string connectionString = "AccountEndpoint=https://localhost:8081;AccountKey=IA==";

        var cosmosClient = Substitute.For<CosmosClient>();

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, cosmosClient);

        using var healthContributor = new CosmosDbHealthContributor(serviceName, serviceProvider, CosmosDbPackageResolver.Default,
            NullLogger<CosmosDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
    }

    [Fact]
    public async Task Canceled_Throws()
    {
        const string serviceName = "Example";
        const string connectionString = "AccountEndpoint=https://localhost:8081;AccountKey=IA==";

        var cosmosClient = Substitute.For<CosmosClient>();

        cosmosClient.ReadAccountAsync().Returns(async _ =>
        {
            await Task.Delay(3.Seconds(), TestContext.Current.CancellationToken);
            return null;
        });

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, cosmosClient);

        using var healthContributor = new CosmosDbHealthContributor(serviceName, serviceProvider, CosmosDbPackageResolver.Default,
            NullLogger<CosmosDbHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // ReSharper disable AccessToDisposedClosure
        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);
        // ReSharper restore AccessToDisposedClosure

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact(Skip = "Integration test - Requires local CosmosDB emulator")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        const string serviceName = "Example";

        const string connectionString =
            "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        var cosmosClient = new CosmosClient(connectionString);

        await using ServiceProvider serviceProvider = CreateServiceProvider(serviceName, connectionString, cosmosClient);

        using var healthContributor = new CosmosDbHealthContributor(serviceName, serviceProvider, CosmosDbPackageResolver.Default,
            NullLogger<CosmosDbHealthContributor>.Instance);

        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Status.Should().Be(HealthStatus.Up);
        result.Details.Should().Contain("host", "localhost");
        result.Details.Should().Contain("service", "Example");
        result.Details.Should().NotContainKey("error");
    }

    private static ServiceProvider CreateServiceProvider(string serviceName, string connectionString, CosmosClient cosmosClient)
    {
        HashSet<string> serviceNames = [serviceName];

        var services = new ServiceCollection();
        services.AddOptions<CosmosDbOptions>(serviceName).Configure(dbOptions => dbOptions.ConnectionString = connectionString);
        services.AddSingleton(provider => new ConnectorFactory<CosmosDbOptions, CosmosClient>(provider, serviceNames, (_, _) => cosmosClient, true));

        return services.BuildServiceProvider(true);
    }
}
