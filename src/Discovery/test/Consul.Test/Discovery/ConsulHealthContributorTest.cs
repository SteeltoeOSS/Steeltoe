// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Moq;
using Steeltoe.Common.HealthChecks;
using Xunit;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulHealthContributorTest
{
    [Fact]
    public async Task GetLeaderStatusAsync_ReturnsExpected()
    {
        var statusMoq = new Mock<IStatusEndpoint>();
        statusMoq.Setup(endpoint => endpoint.Leader(default)).Returns(Task.FromResult("thestatus"));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Status).Returns(statusMoq.Object);

        var healthContributor = new ConsulHealthContributor(clientMoq.Object);
        string result = await healthContributor.GetLeaderStatusAsync(CancellationToken.None);

        Assert.Equal("thestatus", result);
    }

    [Fact]
    public async Task GetCatalogServicesAsync_ReturnsExpected()
    {
        var queryResult = new QueryResult<Dictionary<string, string[]>>
        {
            Response = new Dictionary<string, string[]>
            {
                {
                    "foo", [
                        "I1",
                        "I2"
                    ]
                },
                {
                    "bar", [
                        "I1",
                        "I2"
                    ]
                }
            }
        };

        var catalogMoq = new Mock<ICatalogEndpoint>();
        catalogMoq.Setup(endpoint => endpoint.Services(QueryOptions.Default, default)).Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        var healthContributor = new ConsulHealthContributor(clientMoq.Object);
        Dictionary<string, string[]> result = await healthContributor.GetCatalogServicesAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains("foo", result.Keys);
        Assert.Contains("bar", result.Keys);
    }

    [Fact]
    public async Task Health_ReturnsExpected()
    {
        var queryResult = new QueryResult<Dictionary<string, string[]>>
        {
            Response = new Dictionary<string, string[]>
            {
                {
                    "foo", [
                        "I1",
                        "I2"
                    ]
                },
                {
                    "bar", [
                        "I1",
                        "I2"
                    ]
                }
            }
        };

        var statusMoq = new Mock<IStatusEndpoint>();
        statusMoq.Setup(endpoint => endpoint.Leader(default)).Returns(Task.FromResult("thestatus"));

        var catalogMoq = new Mock<ICatalogEndpoint>();
        catalogMoq.Setup(endpoint => endpoint.Services(QueryOptions.Default, default)).Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Status).Returns(statusMoq.Object);
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        var healthContributor = new ConsulHealthContributor(clientMoq.Object);
        HealthCheckResult? result = await healthContributor.CheckHealthAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Equal(2, result.Details.Count);
        Assert.Contains("leader", result.Details.Keys);
        Assert.Contains("services", result.Details.Keys);
    }
}
