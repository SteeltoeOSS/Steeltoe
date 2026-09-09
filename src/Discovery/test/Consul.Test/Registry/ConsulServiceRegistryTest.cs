// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistryTest
{
    [Fact]
    public async Task RegisterAsync_CallsServiceRegister_AddsHeartbeatToScheduler()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);
        await registry.RegisterAsync(registration, TestContext.Current.CancellationToken);

        await agent.Received(1).ServiceRegister(registration.InnerRegistration, Arg.Any<CancellationToken>());

        scheduler.ServiceHeartbeats.Should().ContainSingle();
        scheduler.ServiceHeartbeats.Should().ContainKey(registration.InstanceId);
    }

    [Fact]
    public async Task DeregisterAsync_CallsServiceDeregister_RemovesHeartbeatFromScheduler()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);
        await registry.RegisterAsync(registration, TestContext.Current.CancellationToken);

        await agent.Received(1).ServiceRegister(registration.InnerRegistration, Arg.Any<CancellationToken>());

        scheduler.ServiceHeartbeats.Should().ContainSingle();
        scheduler.ServiceHeartbeats.Should().ContainKey(registration.InstanceId);

        await registry.DeregisterAsync(registration, TestContext.Current.CancellationToken);

        await agent.Received(1).ServiceDeregister(registration.InnerRegistration.ID, Arg.Any<CancellationToken>());
        scheduler.ServiceHeartbeats.Should().BeEmpty();
    }

    [Fact]
    public async Task SetStatusAsync_ThrowsInvalidStatus()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await registry.SetStatusAsync(registration, string.Empty, TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetStatusAsync_CallsConsulClient()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);
        await registry.SetStatusAsync(registration, "Up", TestContext.Current.CancellationToken);
        await agent.Received(1).DisableServiceMaintenance(registration.InstanceId, Arg.Any<CancellationToken>());

        await registry.SetStatusAsync(registration, "Out_of_Service", TestContext.Current.CancellationToken);
        await agent.Received(1).EnableServiceMaintenance(registration.InstanceId, "OUT_OF_SERVICE", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsExpected()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();

        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar"
        };

        ConsulRegistration registration = TestRegistrationFactory.Create(appSettings);

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

        var client = Substitute.For<IConsulClient>();
        var health = Substitute.For<IHealthEndpoint>();

        client.Health.Returns(health);
        health.Checks(registration.ServiceId, QueryOptions.Default, Arg.Any<CancellationToken>()).Returns(result);

        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, scheduler, NullLogger<ConsulServiceRegistry>.Instance);

        string status = await registry.GetStatusAsync(registration, TestContext.Current.CancellationToken);
        status.Should().Be("OUT_OF_SERVICE");
    }
}
