// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistrarTest
{
    private static readonly WriteResult DefaultWriteResult = new();

    [Fact]
    public async Task Start_CallsRegistry()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        agentMock.Verify(agent => agent.ServiceRegister(registration.InnerRegistration, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Start_DoesNotCallRegistry()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Register = false
        });

        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        agentMock.Verify(agent => agent.ServiceRegister(registration.InnerRegistration, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Start_DoesNotStart()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Enabled = false
        });

        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeFalse();
        agentMock.Verify(agent => agent.ServiceRegister(registration.InnerRegistration, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Dispose_CallsRegistry()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(TestContext.Current.CancellationToken);
        }

        registrar.IsRunning.Should().BeFalse();
        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispose_DoesNotCallRegistry()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Deregister = false
        });

        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (Mock<IConsulClient> clientMock, Mock<IAgentEndpoint> agentMock) = CreateConsulClientAgentMock(registration);
        await using var registry = new ConsulServiceRegistry(clientMock.Object, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(TestContext.Current.CancellationToken);
        }

        agentMock.Verify(agent => agent.ServiceDeregister(registration.InstanceId, It.IsAny<CancellationToken>()), Times.Never);
    }

    private static (Mock<IConsulClient> ClientMock, Mock<IAgentEndpoint> AgentMock) CreateConsulClientAgentMock(ConsulRegistration registration)
    {
        var agentMock = new Mock<IAgentEndpoint>();

        agentMock.Setup(agent => agent.ServiceRegister(registration.InnerRegistration, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(DefaultWriteResult));

        agentMock.Setup(agent => agent.ServiceDeregister(registration.InstanceId, It.IsAny<CancellationToken>())).Returns(Task.FromResult(DefaultWriteResult));

        var clientMock = new Mock<IConsulClient>();
        clientMock.Setup(client => client.Agent).Returns(agentMock.Object);

        return (clientMock, agentMock);
    }
}
