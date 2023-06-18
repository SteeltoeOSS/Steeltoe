// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.CosmosDb;
using Xunit;

namespace Steeltoe.Connectors.Test.CosmosDb;

public sealed class CosmosDbHealthContributorTest
{
    [Fact]
    public void Not_Connected_Returns_Down_Status()
    {
        using var cosmosClient = (IDisposable)Activator.CreateInstance(CosmosDbTypeLocator.CosmosClient,
            "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", null);

        var healthContributor =
            new CosmosDbHealthContributor(cosmosClient, "CosmosDB-myService", "localhost", NullLogger<CosmosDbHealthContributor>.Instance, 1);

        HealthCheckResult status = healthContributor.Health();
        status.Status.Should().Be(HealthStatus.Down);
        status.Description.Should().Be("Failed to open CosmosDB connection!");
        status.Details.Should().Contain("host", "localhost");
    }

    [Fact(Skip = "Integration test - Requires local CosmosDB emulator")]
    public void Is_Connected_Returns_Up_Status()
    {
        using var cosmosClient = (IDisposable)Activator.CreateInstance(CosmosDbTypeLocator.CosmosClient,
            "AccountEndpoint=https://localhost:8081;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", null);

        var healthContributor = new CosmosDbHealthContributor(cosmosClient, "CosmosDB-myService", "localhost", NullLogger<CosmosDbHealthContributor>.Instance);

        HealthCheckResult status = healthContributor.Health();
        status.Status.Should().Be(HealthStatus.Up);
        status.Details.Should().Contain("host", "localhost");
    }
}
