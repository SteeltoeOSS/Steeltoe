// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Consul;
using FluentAssertions;
using Moq;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistrarTest
{
    private static readonly WriteResult DefaultWriteResult = new();

    [Fact]
    public void Register_CallsRegistry()
    {
        var registration = new ConsulRegistration();
        var options = new ConsulDiscoveryOptions();

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Register();

        agentMock.Verify(agent => agent.ServiceRegister(registration.Service, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Deregister_CallsRegistry()
    {
        var registration = new ConsulRegistration();
        var options = new ConsulDiscoveryOptions();

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Deregister();

        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Register_DoesNotCallRegistry()
    {
        var registration = new ConsulRegistration();

        var options = new ConsulDiscoveryOptions
        {
            Register = false
        };

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Register();

        agentMock.Verify(agent => agent.ServiceRegister(registration.Service, CancellationToken.None), Times.Never);
    }

    [Fact]
    public void Deregister_DoesNotCallRegistry()
    {
        var registration = new ConsulRegistration();

        var options = new ConsulDiscoveryOptions
        {
            Deregister = false
        };

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Deregister();

        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, CancellationToken.None), Times.Never);
    }

    [Fact]
    public void Start_DoesNotStart()
    {
        var registration = new ConsulRegistration();

        var options = new ConsulDiscoveryOptions
        {
            Enabled = false
        };

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> _) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Start();

        registrar.IsRunning.Should().Be(0);
    }

    [Fact]
    public void Start_CallsRegistry()
    {
        var registration = new ConsulRegistration();
        var options = new ConsulDiscoveryOptions();

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Start();

        registrar.IsRunning.Should().Be(1);
        agentMock.Verify(agent => agent.ServiceRegister(registration.Service, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void Dispose_CallsRegistry()
    {
        var registration = new ConsulRegistration();
        var options = new ConsulDiscoveryOptions();

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        var registry = new ConsulServiceRegistry(clientMock.Object, options);
        var registrar = new ConsulServiceRegistrar(registry, options, registration);

        registrar.Start();
        registrar.Dispose();

        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, CancellationToken.None), Times.Once);
        registrar.IsRunning.Should().Be(0);
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
