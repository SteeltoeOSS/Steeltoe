// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulDiscoveryClientTest
{
    [Fact]
    public async Task AddInstancesToListAsync_AddsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Register = false,
            HostName = "this.host.com",
            Port = 8888
        };

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

        healthMoq.Setup(endpoint =>
                endpoint.Service("ServiceId", options.DefaultQueryTag, options.QueryPassing, QueryOptions.Default, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Health).Returns(healthMoq.Object);

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var discoveryClient = new ConsulDiscoveryClient(clientMoq.Object, optionsMonitor, NullLoggerFactory.Instance);

        List<IServiceInstance> serviceInstances = [];

        await discoveryClient.AddInstancesToListAsync(serviceInstances, "ServiceId", QueryOptions.Default, optionsMonitor.CurrentValue,
            TestContext.Current.CancellationToken);

        serviceInstances.Should().HaveCount(2);

        serviceInstances[0].Host.Should().Be("foo.bar.com");
        serviceInstances[0].ServiceId.Should().Be("ServiceId");
        serviceInstances[0].IsSecure.Should().BeTrue();
        serviceInstances[0].Port.Should().Be(1234);
        serviceInstances[0].Metadata.Should().HaveCount(2);
        serviceInstances[0].Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        serviceInstances[0].Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("true");
        serviceInstances[0].Uri.Should().Be(new Uri("https://foo.bar.com:1234"));

        serviceInstances[1].Host.Should().Be("foo1.bar1.com");
        serviceInstances[1].ServiceId.Should().Be("ServiceId");
        serviceInstances[1].IsSecure.Should().BeFalse();
        serviceInstances[1].Port.Should().Be(5678);
        serviceInstances[1].Metadata.Should().HaveCount(2);
        serviceInstances[1].Metadata.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        serviceInstances[1].Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("false");
        serviceInstances[1].Uri.Should().Be(new Uri("http://foo1.bar1.com:5678"));
    }

    [Fact]
    public async Task GetServicesAsync_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Register = false,
            HostName = "this.host.com",
            Port = 8888
        };

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
        catalogMoq.Setup(endpoint => endpoint.Services(null, null, QueryOptions.Default, It.IsAny<CancellationToken>())).Returns(Task.FromResult(queryResult));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var discoveryClient = new ConsulDiscoveryClient(clientMoq.Object, optionsMonitor, NullLoggerFactory.Instance);
        ISet<string> serviceIds = await discoveryClient.GetServiceIdsAsync(TestContext.Current.CancellationToken);

        serviceIds.Should().HaveCount(2);
        serviceIds.Should().Contain("foo");
        serviceIds.Should().Contain("bar");
    }

    [Fact]
    public async Task GetAllInstances_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            Register = false,
            HostName = "this.host.com",
            Port = 8888
        };

        var queryResult1 = new QueryResult<Dictionary<string, string[]>>
        {
            Response = new Dictionary<string, string[]>
            {
                ["ServiceId"] =
                [
                    "I1",
                    "I2"
                ]
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
        catalogMoq.Setup(endpoint => endpoint.Services(null, null, QueryOptions.Default, It.IsAny<CancellationToken>())).Returns(Task.FromResult(queryResult1));

        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Catalog).Returns(catalogMoq.Object);

        var healthMoq = new Mock<IHealthEndpoint>();
        clientMoq.Setup(client => client.Health).Returns(healthMoq.Object);

        healthMoq.Setup(h => h.Service("ServiceId", options.DefaultQueryTag, options.QueryPassing, QueryOptions.Default, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(queryResult2));

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var discoveryClient = new ConsulDiscoveryClient(clientMoq.Object, optionsMonitor, NullLoggerFactory.Instance);
        IList<IServiceInstance> serviceInstances = await discoveryClient.GetAllInstancesAsync(QueryOptions.Default, TestContext.Current.CancellationToken);

        serviceInstances.Should().HaveCount(2);

        serviceInstances[0].Host.Should().Be("foo.bar.com");
        serviceInstances[0].ServiceId.Should().Be("ServiceId");
        serviceInstances[0].IsSecure.Should().BeTrue();
        serviceInstances[0].Port.Should().Be(1234);
        serviceInstances[0].Metadata.Should().HaveCount(2);
        serviceInstances[0].Metadata.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        serviceInstances[0].Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("true");
        serviceInstances[0].Uri.Should().Be(new Uri("https://foo.bar.com:1234"));

        serviceInstances[1].Host.Should().Be("foo1.bar1.com");
        serviceInstances[1].ServiceId.Should().Be("ServiceId");
        serviceInstances[1].IsSecure.Should().BeFalse();
        serviceInstances[1].Port.Should().Be(5678);
        serviceInstances[1].Metadata.Should().HaveCount(2);
        serviceInstances[1].Metadata.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        serviceInstances[1].Metadata.Should().ContainKey("secure").WhoseValue.Should().Be("false");
        serviceInstances[1].Uri.Should().Be(new Uri("http://foo1.bar1.com:5678"));
    }
}
