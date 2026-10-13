// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistrarTest
{
    private static readonly WriteResult DefaultWriteResult = new();

    [Fact]
    public async Task Start_CallsRegistry()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, instance, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        await agent.Received(1).ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotCallRegistry()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Register = false
        });

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, instance, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        await agent.DidNotReceive().ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotStart()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Enabled = false
        });

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        await using var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, instance, NullLogger<ConsulServiceRegistrar>.Instance);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeFalse();
        await agent.DidNotReceive().ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispose_CallsRegistry()
    {
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, instance, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(TestContext.Current.CancellationToken);
        }

        registrar.IsRunning.Should().BeFalse();
        await agent.Received(1).ServiceDeregister(instance.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispose_DoesNotCallRegistry()
    {
        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Deregister = false
        });

        ConsulServiceInstance instance = TestRegistrationFactory.CreateServiceInstance(new Dictionary<string, string?>());

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClientAgentSubstitute(instance);
        await using var registry = new ConsulServiceRegistry(client, optionsMonitor, null, NullLogger<ConsulServiceRegistry>.Instance);
        var registrar = new ConsulServiceRegistrar(registry, optionsMonitor, instance, NullLogger<ConsulServiceRegistrar>.Instance);

        await using (registrar)
        {
            await registrar.StartAsync(TestContext.Current.CancellationToken);
        }

        await agent.DidNotReceive().ServiceDeregister(instance.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CreateAgentRegistration_ReturnsExpected()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["spring:application:name"] = "foobar",
            ["consul:discovery:hostName"] = "some-host",
            ["consul:discovery:port"] = "1100",
            ["consul:discovery:scheme"] = "https"
        };

        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions(appSettings);
        ConsulServiceInstance instance = ConsulServiceInstance.FromOptions(options);

        AgentServiceRegistration registration = ConsulServiceRegistrar.CreateAgentRegistration(instance, options);

        registration.Address.Should().Be("some-host");
        registration.ID.Should().StartWith("foobar-");
        registration.Name.Should().Be("foobar");
        registration.Port.Should().Be(1100);
        registration.Check.Should().NotBeNull();
        registration.Tags.Should().NotBeNull();
        registration.Meta.Should().ContainKey("secure").WhoseValue.Should().Be("true");
    }

    [Fact]
    public void CreateAgentRegistration_WithoutPort_HasNoPortAndNoCheck()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions(new Dictionary<string, string?>());
        options.Port = 0;

        ConsulServiceInstance instance = ConsulServiceInstance.FromOptions(options);

        AgentServiceRegistration registration = ConsulServiceRegistrar.CreateAgentRegistration(instance, options);

        registration.Port.Should().Be(0);
        registration.Check.Should().BeNull();
    }

    [Fact]
    public void CreateCheck_ReturnsExpected()
    {
        var options = new ConsulDiscoveryOptions
        {
            HostName = "some-host"
        };

        AgentServiceCheck result = ConsulServiceRegistrar.CreateCheck(1234, options);

        result.Should().NotBeNull();
        result.TTL.Should().Be(DateTimeConversions.ToTimeSpan(options.Heartbeat!.TimeToLive));
        result.DeregisterCriticalServiceAfter.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!));

        options.Heartbeat = null;
        const int port = 1234;
        result = ConsulServiceRegistrar.CreateCheck(port, options);
        var uri = new Uri($"{options.EffectiveScheme}://{options.HostName}:{port}{options.HealthCheckPath}");

        result.HTTP.Should().Be(uri.ToString());
        result.Interval.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckInterval!));
        result.Timeout.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout!));
        result.DeregisterCriticalServiceAfter.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!));
        result.TLSSkipVerify.Should().Be(options.HealthCheckTlsSkipVerify);
    }

    [Fact]
    public void CreateCheck_WhenHealthCheckPathIsSetAndHeartbeatIsDisabled_ThenShouldSetHttp()
    {
        const string path = "/my/custom/health";

        var options = new ConsulDiscoveryOptions
        {
            HostName = "some-host",
            HealthCheckPath = path,
            Heartbeat = new ConsulHeartbeatOptions
            {
                Enabled = false
            }
        };

        AgentServiceCheck check = ConsulServiceRegistrar.CreateCheck(1234, options);

        check.HTTP.Should().Contain(path);
    }

    [Fact]
    public void CreateCheck_WhenHealthCheckPathIsSetAndHeartbeatIsEnabled_ThenHttpShouldBeNull()
    {
        const string path = "/my/custom/health";

        var options = new ConsulDiscoveryOptions
        {
            HealthCheckPath = path,
            Heartbeat = new ConsulHeartbeatOptions
            {
                Enabled = true
            }
        };

        AgentServiceCheck check = ConsulServiceRegistrar.CreateCheck(1234, options);

        check.HTTP.Should().BeNull();
    }

    private static (IConsulClient Client, IAgentEndpoint Agent) CreateConsulClientAgentSubstitute(ConsulServiceInstance instance)
    {
        var agent = Substitute.For<IAgentEndpoint>();
        agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));
        agent.ServiceDeregister(instance.InstanceId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        return (client, agent);
    }
}
