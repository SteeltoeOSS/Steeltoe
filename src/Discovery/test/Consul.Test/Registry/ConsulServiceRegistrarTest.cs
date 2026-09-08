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

public sealed class ConsulServiceRegistrarTest
{
    private static readonly WriteResult DefaultWriteResult = new();

    [Fact]
    public async Task Start_CallsRegistry()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(registration);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        await agent.Received(1).ServiceRegister(registration.InnerRegistration, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotCallRegistry()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Register = false
        });

        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(registration);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        await agent.DidNotReceive().ServiceRegister(registration.InnerRegistration, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotStart()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Enabled = false
        });

        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(registration);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeFalse();
        await agent.DidNotReceive().ServiceRegister(registration.InnerRegistration, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispose_CallsRegistry()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(registration);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(TestContext.Current.CancellationToken);
        }

        registrar.IsRunning.Should().BeFalse();
        await agent.Received(1).ServiceDeregister(registration.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispose_DoesNotCallRegistry()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Deregister = false
        });

        ConsulRegistration registration = TestRegistrationFactory.Create(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(registration);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, registration, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(TestContext.Current.CancellationToken);
        }

        await agent.DidNotReceive().ServiceDeregister(registration.InstanceId, Arg.Any<CancellationToken>());
    }

    private static (IConsulClient Client, IAgentEndpoint Agent) CreateConsulClientAgentSubstitute(ConsulRegistration registration)
    {
        var agent = Substitute.For<IAgentEndpoint>();
        agent.ServiceRegister(registration.InnerRegistration, Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));
        agent.ServiceDeregister(registration.InstanceId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        return (client, agent);
    }
}
