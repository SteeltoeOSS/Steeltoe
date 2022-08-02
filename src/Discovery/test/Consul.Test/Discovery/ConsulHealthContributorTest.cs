// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Moq;
using Steeltoe.Common.HealthChecks;
using Xunit;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Discovery.Test;

public class ConsulHealthContributorTest
{
    [Fact]
    public void Constructor_ThrowsIfNulls()
    {
        var clientMoq = new Mock<IConsulClient>();
        Assert.Throws<ArgumentNullException>(() => new ConsulHealthContributor(null, new ConsulDiscoveryOptions()));
        Assert.Throws<ArgumentNullException>(() => new ConsulHealthContributor(clientMoq.Object, (ConsulDiscoveryOptions)null));
    }

    [Fact]
    public async Task GetLeaderStatusAsync_ReturnsExpected()
    {
        Task<string> statusResult = Task.FromResult("thestatus");
        var clientMoq = new Mock<IConsulClient>();
        var statusMoq = new Mock<IStatusEndpoint>();
        clientMoq.Setup(c => c.Status).Returns(statusMoq.Object);
        statusMoq.Setup(s => s.Leader(default)).Returns(statusResult);

        var contrib = new ConsulHealthContributor(clientMoq.Object, new ConsulDiscoveryOptions());
        string result = await contrib.GetLeaderStatusAsync();
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
                    "foo", new[]
                    {
                        "I1",
                        "I2"
                    }
                },
                {
                    "bar", new[]
                    {
                        "I1",
                        "I2"
                    }
                }
            }
        };

        Task<QueryResult<Dictionary<string, string[]>>> catResult = Task.FromResult(queryResult);
        var clientMoq = new Mock<IConsulClient>();
        var catMoq = new Mock<ICatalogEndpoint>();
        clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
        catMoq.Setup(c => c.Services(QueryOptions.Default, default)).Returns(catResult);

        var contrib = new ConsulHealthContributor(clientMoq.Object, new ConsulDiscoveryOptions());
        Dictionary<string, string[]> result = await contrib.GetCatalogServicesAsync();
        Assert.Equal(2, result.Count);
        Assert.Contains("foo", result.Keys);
        Assert.Contains("bar", result.Keys);
    }

    [Fact]
    public void Health_ReturnsExpected()
    {
        var queryResult = new QueryResult<Dictionary<string, string[]>>
        {
            Response = new Dictionary<string, string[]>
            {
                {
                    "foo", new[]
                    {
                        "I1",
                        "I2"
                    }
                },
                {
                    "bar", new[]
                    {
                        "I1",
                        "I2"
                    }
                }
            }
        };

        Task<QueryResult<Dictionary<string, string[]>>> catResult = Task.FromResult(queryResult);
        Task<string> statusResult = Task.FromResult("thestatus");

        var clientMoq = new Mock<IConsulClient>();
        var catMoq = new Mock<ICatalogEndpoint>();
        var statusMoq = new Mock<IStatusEndpoint>();
        clientMoq.Setup(c => c.Status).Returns(statusMoq.Object);
        clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
        statusMoq.Setup(s => s.Leader(default)).Returns(statusResult);
        catMoq.Setup(c => c.Services(QueryOptions.Default, default)).Returns(catResult);

        var contrib = new ConsulHealthContributor(clientMoq.Object, new ConsulDiscoveryOptions());
        HealthCheckResult result = contrib.Health();

        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Equal(2, result.Details.Count);

        Assert.Contains("leader", result.Details.Keys);
        Assert.Contains("services", result.Details.Keys);
    }
}
