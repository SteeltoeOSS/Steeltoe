// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.CloudFoundry.Test.Discovery;

public sealed class ConsulDiscoveryClientTest
{
    [Fact]
    public async Task AddInstancesToListAsync_AddsExpected()
    {
        var options = new ConsulDiscoveryOptions();

        var queryResult = new QueryResult<ServiceEntry[]>
        {
            Response =
            [
                new ServiceEntry
                {
                    Service = new AgentService
                    {
                        Service = "ServiceId",
                        Address = "foo.bar.com",
                        Port = 1234,
                        Meta = new Dictionary<string, string>
                        {
                            ["foo"] = "bar",
                            ["secure"] = "true"
                        }
                    }
                },
                new ServiceEntry
                {
                    Service = new AgentService
                    {
                        Service = "ServiceId",
                        Address = "foo1.bar1.com",
                        Port = 5678,
                        Meta = new Dictionary<string, string>
                        {
                            ["bar"] = "foo",
                            ["secure"] = "false"
                        }
                    }
                }
            ]
        };

        var healthMoq = new Mock<IHealthEndpoint>();

        healthMoq.Setup(endpoint => endpoint.Service("ServiceId", options.DefaultQueryTag, options.QueryPassing, QueryOptions.Default, default))
            .Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Health).Returns(healthMoq.Object);

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var discoveryClient = new ConsulDiscoveryClient(clientMoq.Object, optionsMonitor, null, NullLogger<ConsulDiscoveryClient>.Instance);

        List<IServiceInstance> serviceInstances = [];
        await discoveryClient.AddInstancesToListAsync(serviceInstances, "ServiceId", QueryOptions.Default, optionsMonitor.CurrentValue, CancellationToken.None);
        Assert.Equal(2, serviceInstances.Count);

        Assert.Equal("foo.bar.com", serviceInstances[0].Host);
        Assert.Equal("ServiceId", serviceInstances[0].ServiceId);
        Assert.True(serviceInstances[0].IsSecure);
        Assert.Equal(1234, serviceInstances[0].Port);
        Assert.Equal(2, serviceInstances[0].Metadata.Count);
        Assert.Contains("foo", serviceInstances[0].Metadata.Keys);
        Assert.Contains("secure", serviceInstances[0].Metadata.Keys);
        Assert.Contains("bar", serviceInstances[0].Metadata.Values);
        Assert.Contains("true", serviceInstances[0].Metadata.Values);
        Assert.Equal(new Uri("https://foo.bar.com:1234"), serviceInstances[0].Uri);

        Assert.Equal("foo1.bar1.com", serviceInstances[1].Host);
        Assert.Equal("ServiceId", serviceInstances[1].ServiceId);
        Assert.False(serviceInstances[1].IsSecure);
        Assert.Equal(5678, serviceInstances[1].Port);
        Assert.Equal(2, serviceInstances[1].Metadata.Count);
        Assert.Contains("bar", serviceInstances[1].Metadata.Keys);
        Assert.Contains("secure", serviceInstances[1].Metadata.Keys);
        Assert.Contains("foo", serviceInstances[1].Metadata.Values);
        Assert.Contains("false", serviceInstances[1].Metadata.Values);
        Assert.Equal(new Uri("http://foo1.bar1.com:5678"), serviceInstances[1].Uri);
    }

    [Fact]
    public async Task GetServicesAsync_ReturnsExpected()
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

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var discoveryClient = new ConsulDiscoveryClient(clientMoq.Object, optionsMonitor, null, NullLogger<ConsulDiscoveryClient>.Instance);
        ISet<string> serviceIds = await discoveryClient.GetServiceIdsAsync(QueryOptions.Default, CancellationToken.None);

        Assert.Equal(2, serviceIds.Count);
        Assert.Contains("foo", serviceIds);
        Assert.Contains("bar", serviceIds);
    }

    [Fact]
    public async Task GetAllInstances_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions();

        var queryResult1 = new QueryResult<Dictionary<string, string[]>>
        {
            Response = new Dictionary<string, string[]>
            {
                {
                    "ServiceId", [
                        "I1",
                        "I2"
                    ]
                }
            }
        };

        var queryResult2 = new QueryResult<ServiceEntry[]>
        {
            Response =
            [
                new ServiceEntry
                {
                    Service = new AgentService
                    {
                        Service = "ServiceId",
                        Address = "foo.bar.com",
                        Port = 1234,
                        Meta = new Dictionary<string, string>
                        {
                            ["foo"] = "bar",
                            ["secure"] = "true"
                        }
                    }
                },
                new ServiceEntry
                {
                    Service = new AgentService
                    {
                        Service = "ServiceId",
                        Address = "foo1.bar1.com",
                        Port = 5678,
                        Meta = new Dictionary<string, string>
                        {
                            ["bar"] = "foo",
                            ["secure"] = "false"
                        }
                    }
                }
            ]
        };

        var catalogMoq = new Mock<ICatalogEndpoint>();
        catalogMoq.Setup(endpoint => endpoint.Services(QueryOptions.Default, default)).Returns(Task.FromResult(queryResult1));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        var healthMoq = new Mock<IHealthEndpoint>();
        clientMoq.Setup(client => client.Health).Returns(healthMoq.Object);

        healthMoq.Setup(h => h.Service("ServiceId", options.DefaultQueryTag, options.QueryPassing, QueryOptions.Default, default))
            .Returns(Task.FromResult(queryResult2));

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var discoveryClient = new ConsulDiscoveryClient(clientMoq.Object, optionsMonitor, null, NullLogger<ConsulDiscoveryClient>.Instance);
        IList<IServiceInstance> serviceInstances = await discoveryClient.GetAllInstancesAsync(QueryOptions.Default, CancellationToken.None);

        Assert.Equal(2, serviceInstances.Count);

        Assert.Equal("foo.bar.com", serviceInstances[0].Host);
        Assert.Equal("ServiceId", serviceInstances[0].ServiceId);
        Assert.True(serviceInstances[0].IsSecure);
        Assert.Equal(1234, serviceInstances[0].Port);
        Assert.Equal(2, serviceInstances[0].Metadata.Count);
        Assert.Contains("foo", serviceInstances[0].Metadata.Keys);
        Assert.Contains("secure", serviceInstances[0].Metadata.Keys);
        Assert.Contains("bar", serviceInstances[0].Metadata.Values);
        Assert.Contains("true", serviceInstances[0].Metadata.Values);
        Assert.Equal(new Uri("https://foo.bar.com:1234"), serviceInstances[0].Uri);

        Assert.Equal("foo1.bar1.com", serviceInstances[1].Host);
        Assert.Equal("ServiceId", serviceInstances[1].ServiceId);
        Assert.False(serviceInstances[1].IsSecure);
        Assert.Equal(5678, serviceInstances[1].Port);
        Assert.Equal(2, serviceInstances[1].Metadata.Count);
        Assert.Contains("bar", serviceInstances[1].Metadata.Keys);
        Assert.Contains("secure", serviceInstances[1].Metadata.Keys);
        Assert.Contains("foo", serviceInstances[1].Metadata.Values);
        Assert.Contains("false", serviceInstances[1].Metadata.Values);
        Assert.Equal(new Uri("http://foo1.bar1.com:5678"), serviceInstances[1].Uri);
    }
}
