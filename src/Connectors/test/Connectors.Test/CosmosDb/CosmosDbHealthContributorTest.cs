// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.CosmosDb;
using Xunit;

namespace Steeltoe.Connectors.Test.CosmosDb;

public sealed class CosmosDbHealthContributorTest
{
    [Fact]
    public async Task Not_Connected_Returns_Down_Status()
    {
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        httpMessageHandlerMock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Throws(new HttpRequestException("No connection could be made because ..."));

        var options = new CosmosClientOptions
        {
            HttpClientFactory = () => new HttpClient(httpMessageHandlerMock.Object)
        };

        var cosmosClient = new CosmosClient("AccountEndpoint=https://localhost:8081;AccountKey=IA==", options);

        using var healthContributor = new CosmosDbHealthContributor(cosmosClient, "localhost", NullLogger<CosmosDbHealthContributor>.Instance)
        {
            ServiceName = "Example"
        };

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("CosmosDB health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "HttpRequestException: No connection could be made because ...");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact]
    public async Task Not_Connected_With_Timeout_Returns_Down_Status()
    {
        var cosmosClient = new CosmosClient("AccountEndpoint=https://localhost:8081;AccountKey=IA==");

        using var healthContributor = new CosmosDbHealthContributor(cosmosClient, "localhost", NullLogger<CosmosDbHealthContributor>.Instance);
        healthContributor.ServiceName = "Example";
        healthContributor.Timeout = 1.Milliseconds();

        HealthCheckResult? status = await healthContributor.CheckHealthAsync(CancellationToken.None);

        status.Should().NotBeNull();
        status!.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("CosmosDB health check failed");
        status.Details.Should().Contain("host", "localhost");
        status.Details.Should().Contain("service", "Example");
        status.Details.Should().Contain("error", "TimeoutException: The operation has timed out.");
        status.Details.Should().Contain("status", "DOWN");
    }

    [Fact]
    public async Task Is_Connected_Returns_Up_Status()
    {
        var cosmosClientMock = new Mock<CosmosClient>();

        using var healthContributor = new CosmosDbHealthContributor(cosmosClientMock.Object, "localhost", NullLogger<CosmosDbHealthContributor>.Instance)
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
        var cosmosClientMock = new Mock<CosmosClient>();

        cosmosClientMock.Setup(client => client.ReadAccountAsync()).Returns(async () =>
        {
            await Task.Delay(3.Seconds());
            return null!;
        });

        using var healthContributor = new CosmosDbHealthContributor(cosmosClientMock.Object, "localhost", NullLogger<CosmosDbHealthContributor>.Instance);

        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        Func<Task> action = async () => await healthContributor.CheckHealthAsync(source.Token);

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact(Skip = "Integration test - Requires local CosmosDB emulator")]
    public async Task Integration_Is_Connected_Returns_Up_Status()
    {
        var cosmosClient = new CosmosClient(
            "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

        using var healthContributor = new CosmosDbHealthContributor(cosmosClient, "localhost", NullLogger<CosmosDbHealthContributor>.Instance)
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
