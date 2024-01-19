// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Consul;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistrarTest
{
    private static readonly WriteResult DefaultWriteResult = new();

    [Fact]
    public async Task Start_CallsRegistry()
    {
        var registration = new ConsulRegistration();
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(CancellationToken.None);

        registrar.IsRunning.Should().Be(1);
        agentMock.Verify(agent => agent.ServiceRegister(registration.Service, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Start_DoesNotCallRegistry()
    {
        var registration = new ConsulRegistration();

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Register = false
        });

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(CancellationToken.None);

        registrar.IsRunning.Should().Be(1);
        agentMock.Verify(agent => agent.ServiceRegister(registration.Service, CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Start_DoesNotStart()
    {
        var registration = new ConsulRegistration();

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Enabled = false
        });

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(CancellationToken.None);

        registrar.IsRunning.Should().Be(0);
        agentMock.Verify(agent => agent.ServiceRegister(registration.Service, CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Dispose_CallsRegistry()
    {
        var registration = new ConsulRegistration();
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(CancellationToken.None);
        }

        registrar.IsRunning.Should().Be(0);
        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Dispose_DoesNotCallRegistry()
    {
        var registration = new ConsulRegistration();

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Deregister = false
        });

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(CancellationToken.None);
        }

        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, CancellationToken.None), Times.Never);
    }

    private static (Mock<IConsulClient> ClientMock, Mock<IAgentEndpoint> AgentMock) CreateConsulClientAgentMock(ConsulRegistration registration)
    {
        var agentMock = new Mock<IAgentEndpoint>();
        agentMock.Setup(agent => agent.ServiceRegister(registration.Service, CancellationToken.None)).Returns(Task.FromResult(DefaultWriteResult));
        agentMock.Setup(agent => agent.ServiceDeregister(registration.InstanceId, CancellationToken.None)).Returns(Task.FromResult(DefaultWriteResult));

        var clientMock = new Mock<IConsulClient>();
        clientMock.Setup(client => client.Agent).Returns(agentMock.Object);

        return (clientMock, agentMock);
    }
}
