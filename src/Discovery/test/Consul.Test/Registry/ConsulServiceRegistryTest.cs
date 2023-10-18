// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Configuration;
using Moq;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistryTest
{
    [Fact]
    public void Constructor_ThrowsOnNulls()
    {
        var clientMoq = new Mock<IConsulClient>();
        Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistry(null, new ConsulDiscoveryOptions()));
        Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistry(clientMoq.Object, (ConsulDiscoveryOptions)null));
    }

    [Fact]
    public async Task RegisterAsync_ThrowsOnNull()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await reg.RegisterAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_CallsServiceRegister_AddsHeartbeatToScheduler()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(configurationRoot));
        await reg.RegisterAsync(registration, CancellationToken.None);

        agentMoq.Verify(a => a.ServiceRegister(registration.Service, default), Times.Once);

        Assert.Single(sch.ServiceHeartbeats);
        Assert.Contains(registration.InstanceId, sch.ServiceHeartbeats.Keys);
        sch.Remove(registration.InstanceId);
    }

    [Fact]
    public async Task DeregisterAsync_ThrowsOnNull()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await reg.DeregisterAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task DeregisterAsync_CallsServiceDeregister_RemovesHeartbeatFromScheduler()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(configurationRoot));
        await reg.RegisterAsync(registration, CancellationToken.None);

        agentMoq.Verify(a => a.ServiceRegister(registration.Service, default), Times.Once);

        Assert.Single(sch.ServiceHeartbeats);
        Assert.Contains(registration.InstanceId, sch.ServiceHeartbeats.Keys);

        await reg.DeregisterAsync(registration, CancellationToken.None);
        agentMoq.Verify(a => a.ServiceDeregister(registration.Service.ID, default), Times.Once);
        Assert.Empty(sch.ServiceHeartbeats);
    }

    [Fact]
    public async Task SetStatusAsync_ThrowsOnNull()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await reg.SetStatusAsync(null, string.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task SetStatusAsync_ThrowsInvalidStatus()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(configurationRoot));

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
        await Assert.ThrowsAsync<ArgumentException>(async () => await reg.SetStatusAsync(registration, string.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task SetStatusAsync_CallsConsulClient()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(configurationRoot));

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
        await reg.SetStatusAsync(registration, "Up", CancellationToken.None);
        agentMoq.Verify(a => a.DisableServiceMaintenance(registration.InstanceId, default), Times.Once);
        await reg.SetStatusAsync(registration, "Out_of_Service", CancellationToken.None);
        agentMoq.Verify(a => a.EnableServiceMaintenance(registration.InstanceId, "OUT_OF_SERVICE", default), Times.Once);
    }

    [Fact]
    public async Task GetStatusAsync_ThrowsOnNull()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, clientMoq.Object);

        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await reg.GetStatusAsync(null, CancellationToken.None));
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsExpected()
    {
        var opts = new ConsulDiscoveryOptions();

        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "foobar" }
        });

        IConfigurationRoot configurationRoot = builder.Build();
        var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(configurationRoot));

        var queryResult = new QueryResult<HealthCheck[]>
        {
            Response = new[]
            {
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
            }
        };

        Task<QueryResult<HealthCheck[]>> result = Task.FromResult(queryResult);

        var clientMoq = new Mock<IConsulClient>();
        var healthMoq = new Mock<IHealthEndpoint>();

        clientMoq.Setup(c => c.Health).Returns(healthMoq.Object);
        healthMoq.Setup(h => h.Checks(registration.ServiceId, QueryOptions.Default, default)).Returns(result);

        var sch = new TtlScheduler(opts, clientMoq.Object);
        var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);

        object ret = await reg.GetStatusAsync(registration, CancellationToken.None);
        Assert.Equal("OUT_OF_SERVICE", ret);
    }
}
