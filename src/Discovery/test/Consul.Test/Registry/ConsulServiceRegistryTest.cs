// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistryTest
{
    [Fact]
    public async Task RegisterAsync_CallsServiceRegister_AddsHeartbeatToScheduler()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        await using var registry = new ConsulServiceRegistry(clientMoq.Object, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        });

        IConfiguration configuration = builder.Build();
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));
        await registry.RegisterAsync(registration, CancellationToken.None);

        agentMoq.Verify(endpoint => endpoint.ServiceRegister(registration.InnerRegistration, default), Times.Once);

        Assert.Single(scheduler.ServiceHeartbeats);
        Assert.Contains(registration.InstanceId, scheduler.ServiceHeartbeats.Keys);
        await scheduler.RemoveAsync(registration.InstanceId);
    }

    [Fact]
    public async Task DeregisterAsync_CallsServiceDeregister_RemovesHeartbeatFromScheduler()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        await using var registry = new ConsulServiceRegistry(clientMoq.Object, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        });

        IConfiguration configuration = builder.Build();
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));
        await registry.RegisterAsync(registration, CancellationToken.None);

        agentMoq.Verify(endpoint => endpoint.ServiceRegister(registration.InnerRegistration, default), Times.Once);

        Assert.Single(scheduler.ServiceHeartbeats);
        Assert.Contains(registration.InstanceId, scheduler.ServiceHeartbeats.Keys);

        await registry.DeregisterAsync(registration, CancellationToken.None);

        agentMoq.Verify(endpoint => endpoint.ServiceDeregister(registration.InnerRegistration.ID, default), Times.Once);
        Assert.Empty(scheduler.ServiceHeartbeats);
    }

    [Fact]
    public async Task SetStatusAsync_ThrowsInvalidStatus()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        });

        IConfiguration configuration = builder.Build();
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));

        await using var registry = new ConsulServiceRegistry(clientMoq.Object, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);
        await Assert.ThrowsAsync<ArgumentException>(async () => await registry.SetStatusAsync(registration, string.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task SetStatusAsync_CallsConsulClient()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        });

        IConfiguration configuration = builder.Build();
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));

        await using var registry = new ConsulServiceRegistry(clientMoq.Object, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);
        await registry.SetStatusAsync(registration, "Up", CancellationToken.None);
        agentMoq.Verify(endpoint => endpoint.DisableServiceMaintenance(registration.InstanceId, default), Times.Once);

        await registry.SetStatusAsync(registration, "Out_of_Service", CancellationToken.None);
        agentMoq.Verify(endpoint => endpoint.EnableServiceMaintenance(registration.InstanceId, "OUT_OF_SERVICE", default), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsExpected()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "spring:application:name", "foobar" }
        });

        IConfiguration configuration = builder.Build();
        var registration = ConsulRegistration.Create(optionsMonitor, new ApplicationInstanceInfo(configuration));

        var queryResult = new QueryResult<HealthCheck[]>
        {
            Response =
            [
                new HealthCheck
                {
                    ServiceID = registration.InstanceId,
                    Name = "Service Maintenance Mode"
                },
                new HealthCheck
                {
                    ServiceID = "foobar",
                    Name = "Service Maintenance Mode"
                }
            ]
        };

        Task<QueryResult<HealthCheck[]>> result = Task.FromResult(queryResult);

        var clientMoq = new Mock<IConsulClient>();
        var healthMoq = new Mock<IHealthEndpoint>();

        clientMoq.Setup(client => client.Health).Returns(healthMoq.Object);
        healthMoq.Setup(endpoint => endpoint.Checks(registration.ServiceId, QueryOptions.Default, default)).Returns(result);

        await using var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        await using var registry = new ConsulServiceRegistry(clientMoq.Object, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        string status = await registry.GetStatusAsync(registration, CancellationToken.None);
        Assert.Equal("OUT_OF_SERVICE", status);
    }
}
