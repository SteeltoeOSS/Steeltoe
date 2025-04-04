// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Moq;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulHealthContributorTest
{
    [Fact]
    public async Task GetLeaderStatusAsync_ReturnsExpected()
    {
        var statusMoq = new Mock<IStatusEndpoint>();
        statusMoq.Setup(endpoint => endpoint.Leader(It.IsAny<CancellationToken>())).Returns(Task.FromResult("the-status"));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Status).Returns(statusMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var healthContributor = new ConsulHealthContributor(clientMoq.Object, optionsMonitor);
        string result = await healthContributor.GetLeaderStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal("the-status", result);
    }

    [Fact]
    public async Task GetCatalogServicesAsync_ReturnsExpected()
    {
        var queryResult = new QueryResult<Dictionary<string, string[]>>
        {
            Response = new Dictionary<string, string[]>
            {
                ["foo"] =
                [
                    "I1",
                    "I2"
                ],
                ["bar"] =
                [
                    "I1",
                    "I2"
                ]
            }
        };

        var catalogMoq = new Mock<ICatalogEndpoint>();
        catalogMoq.Setup(endpoint => endpoint.Services(QueryOptions.Default, It.IsAny<CancellationToken>())).Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var healthContributor = new ConsulHealthContributor(clientMoq.Object, optionsMonitor);
        Dictionary<string, string[]> result = await healthContributor.GetCatalogServicesAsync(TestContext.Current.CancellationToken);

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
                ["foo"] =
                [
                    "I1",
                    "I2"
                ],

                ["bar"] =
                [
                    "I1",
                    "I2"
                ]
            }
        };

        var statusMoq = new Mock<IStatusEndpoint>();
        statusMoq.Setup(endpoint => endpoint.Leader(It.IsAny<CancellationToken>())).Returns(Task.FromResult("the-status"));

        var catalogMoq = new Mock<ICatalogEndpoint>();
        catalogMoq.Setup(endpoint => endpoint.Services(QueryOptions.Default, It.IsAny<CancellationToken>())).Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Status).Returns(statusMoq.Object);
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var healthContributor = new ConsulHealthContributor(clientMoq.Object, optionsMonitor);
        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Equal(2, result.Details.Count);
        Assert.Contains("leader", result.Details.Keys);
        Assert.Contains("services", result.Details.Keys);
    }

    [Fact]
    public async Task CheckHealthAsync_ConsulDisabled()
    {
        var options = new ConsulDiscoveryOptions
        {
            Enabled = false
        };

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Catalog).Throws<NotImplementedException>();

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var healthContributor = new ConsulHealthContributor(clientMoq.Object, optionsMonitor);
        HealthCheckResult? result = await healthContributor.CheckHealthAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }
}
