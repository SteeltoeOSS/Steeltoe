// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
    public async Task Start_RegistersAndStartsHeartbeat()
    {
        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        registrar.Heartbeat.Should().NotBeNull();

        string instanceId = registrar.ServiceInstance.InstanceId;

        await agent.Received(1).ServiceRegister(Arg.Is<AgentServiceRegistration>(registration => registration.ID == instanceId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotRegister_WhenRegisterIsFalse()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.Register = false;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeTrue();
        registrar.Heartbeat.Should().BeNull();

        optionsMonitor.ListenerCount.Should().Be(0);
        await agent.DidNotReceive().ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotStart_WhenDisabled()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.Enabled = false;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeFalse();
        registrar.Heartbeat.Should().BeNull();

        optionsMonitor.ListenerCount.Should().Be(0);
        await agent.DidNotReceive().ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_DoesNotStartHeartbeat_WhenHeartbeatIsDisabled()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.Heartbeat!.Enabled = false;

        (IConsulClient client, _) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.Heartbeat.Should().BeNull();
        optionsMonitor.ListenerCount.Should().Be(0);
    }

    [Fact]
    public async Task OptionsChange_WithAutoChangingInstanceId_UpdatesIntervalOfRunningHeartbeat()
    {
        ConsulDiscoveryOptions initialOptions = TestRegistrationFactory.CreateDiscoveryOptions();
        initialOptions.Heartbeat!.TtlValue = 5;

        (IConsulClient client, _) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(initialOptions);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        PeriodicHeartbeat heartbeat = registrar.Heartbeat!;
        TimeSpan beforeInterval = heartbeat.Interval;

        // PostConfigure generates a new instance ID for every options build when none is configured.
        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions();
        changedOptions.Heartbeat!.TtlValue = 10;
        changedOptions.InstanceId.Should().NotBe(initialOptions.InstanceId);

        optionsMonitor.Change(changedOptions);

        registrar.Heartbeat.Should().BeSameAs(heartbeat);
        heartbeat.Interval.Should().NotBe(beforeInterval);
        optionsMonitor.ListenerCount.Should().Be(1);
    }

    [Theory]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public async Task OptionsChange_WhenNoHeartbeatWasStarted_CreatesNothing(bool enabled, bool register, bool heartbeatEnabled, bool registrationFails)
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.Enabled = enabled;
        options.Register = register;
        options.Heartbeat!.Enabled = heartbeatEnabled;
        options.FailFast = false;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        if (registrationFails)
        {
            agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>())
                .Returns<Task<WriteResult>>(_ => throw new HttpRequestException());
        }

        await registrar.StartAsync(TestContext.Current.CancellationToken);
        optionsMonitor.Change(TestRegistrationFactory.CreateDiscoveryOptions());

        registrar.Heartbeat.Should().BeNull();
        optionsMonitor.ListenerCount.Should().Be(0);
    }

    [Fact]
    public async Task Shutdown_ReleasesHeartbeatAndSubscription_AndDeregisters()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);
        optionsMonitor.ListenerCount.Should().Be(1);

        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeFalse();
        registrar.Heartbeat.Should().BeNull();

        optionsMonitor.ListenerCount.Should().Be(0);
        await agent.Received(1).ServiceDeregister(registrar.ServiceInstance.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shutdown_DoesNotDeregister_WhenDeregisterIsFalse()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.Deregister = false;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, TestOptionsMonitor.Create(options));

        await registrar.StartAsync(TestContext.Current.CancellationToken);
        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        registrar.Heartbeat.Should().BeNull();
        await agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shutdown_Deregisters_EvenWhenOptionsChangedToNoLongerRegister()
    {
        ConsulDiscoveryOptions initialOptions = TestRegistrationFactory.CreateDiscoveryOptions();
        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(initialOptions);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions();
        changedOptions.Register = false;
        optionsMonitor.Change(changedOptions);

        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await agent.Received(1).ServiceDeregister(registrar.ServiceInstance.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shutdown_DoesNotDeregisterButStillReleasesResources_WhenOptionsChangedToNotDeregister()
    {
        ConsulDiscoveryOptions initialOptions = TestRegistrationFactory.CreateDiscoveryOptions();
        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(initialOptions);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.Heartbeat.Should().NotBeNull();
        optionsMonitor.ListenerCount.Should().Be(1);

        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions();
        changedOptions.Deregister = false;
        optionsMonitor.Change(changedOptions);

        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        registrar.IsRunning.Should().BeFalse();
        registrar.Heartbeat.Should().BeNull();
        optionsMonitor.ListenerCount.Should().Be(0);
        await agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shutdown_CalledTwice_DeregistersOnce()
    {
        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client);

        await registrar.StartAsync(TestContext.Current.CancellationToken);
        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);
        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await agent.Received(1).ServiceDeregister(registrar.ServiceInstance.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_WhenRegistrationFailsWithFailFast_ThrowsAndLeavesNothingSubscribed()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.FailFast = true;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>())
            .Returns<Task<WriteResult>>(_ => throw new HttpRequestException());

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await registrar.StartAsync(TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<HttpRequestException>();

        registrar.Heartbeat.Should().BeNull();
        optionsMonitor.ListenerCount.Should().Be(0);
    }

    [Fact]
    public async Task Start_WhenRegistrationFailsWithoutFailFast_LeavesNothingBehindAndDoesNotDeregister()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.FailFast = false;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, optionsMonitor);

        agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>())
            .Returns<Task<WriteResult>>(_ => throw new HttpRequestException());

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        registrar.Heartbeat.Should().BeNull();
        optionsMonitor.ListenerCount.Should().Be(0);

        await registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_WithRetry_SucceedsOnLaterAttempt()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.FailFast = true;
        options.Retry.Enabled = true;
        options.Retry.InitialInterval = 1;
        options.Retry.MaxInterval = 1;
        options.Retry.MaxAttempts = 3;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, TestOptionsMonitor.Create(options));

        int attempts = 0;

        agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            attempts++;
            return attempts < 3 ? throw new HttpRequestException() : Task.FromResult(DefaultWriteResult);
        });

        await registrar.StartAsync(TestContext.Current.CancellationToken);

        attempts.Should().Be(3);
        registrar.Heartbeat.Should().NotBeNull();
    }

    [Fact]
    public async Task Start_WithRetry_ThrowsAfterMaxAttempts()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.FailFast = true;
        options.Retry.Enabled = true;
        options.Retry.InitialInterval = 1;
        options.Retry.MaxInterval = 1;
        options.Retry.MaxAttempts = 3;

        (IConsulClient client, IAgentEndpoint agent) = CreateConsulClient();
        await using ConsulServiceRegistrar registrar = CreateRegistrar(client, TestOptionsMonitor.Create(options));

        agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>())
            .Returns<Task<WriteResult>>(_ => throw new HttpRequestException());

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await registrar.StartAsync(TestContext.Current.CancellationToken);

        await action.Should().ThrowExactlyAsync<HttpRequestException>();

        await agent.Received(3).ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
        registrar.Heartbeat.Should().BeNull();
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
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.Port = 0;

        ConsulServiceInstance instance = ConsulServiceInstance.FromOptions(options);

        AgentServiceRegistration registration = ConsulServiceRegistrar.CreateAgentRegistration(instance, options);

        registration.Port.Should().Be(0);
        registration.Check.Should().BeNull();
    }

    [Fact]
    public void CreateCheck_ReturnsExpected()
    {
        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.HostName = "some-host";

        AgentServiceCheck check = ConsulServiceRegistrar.CreateCheck(1234, options);

        check.Should().NotBeNull();
        check.TTL.Should().Be(DateTimeConversions.ToTimeSpan(options.Heartbeat!.TimeToLive));
        check.DeregisterCriticalServiceAfter.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!));

        options.Heartbeat = null;
        const int port = 1234;
        check = ConsulServiceRegistrar.CreateCheck(port, options);
        var uri = new Uri($"{options.EffectiveScheme}://{options.HostName}:{port}{options.HealthCheckPath}");

        check.HTTP.Should().Be(uri.ToString());
        check.Interval.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckInterval!));
        check.Timeout.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout!));
        check.DeregisterCriticalServiceAfter.Should().Be(DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout!));
        check.TLSSkipVerify.Should().Be(options.HealthCheckTlsSkipVerify);
    }

    [Fact]
    public void CreateCheck_WhenHealthCheckPathIsSetAndHeartbeatIsDisabled_ThenShouldSetHttp()
    {
        const string path = "/my/custom/health";

        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.HostName = "some-host";
        options.HealthCheckPath = path;
        options.Heartbeat!.Enabled = false;

        AgentServiceCheck check = ConsulServiceRegistrar.CreateCheck(1234, options);

        check.HTTP.Should().Contain(path);
    }

    [Fact]
    public void CreateCheck_WhenHealthCheckPathIsSetAndHeartbeatIsEnabled_ThenHttpShouldBeNull()
    {
        const string path = "/my/custom/health";

        ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
        options.HealthCheckPath = path;
        options.Heartbeat!.Enabled = true;

        AgentServiceCheck check = ConsulServiceRegistrar.CreateCheck(1234, options);

        check.HTTP.Should().BeNull();
    }

    private static ConsulServiceRegistrar CreateRegistrar(IConsulClient client, IOptionsMonitor<ConsulDiscoveryOptions>? optionsMonitor = null,
        ILoggerFactory? loggerFactory = null)
    {
        if (optionsMonitor == null)
        {
            ConsulDiscoveryOptions options = TestRegistrationFactory.CreateDiscoveryOptions();
            optionsMonitor = TestOptionsMonitor.Create(options);
        }

        loggerFactory ??= NullLoggerFactory.Instance;
        return new ConsulServiceRegistrar(client, optionsMonitor, loggerFactory);
    }

    private static (IConsulClient Client, IAgentEndpoint Agent) CreateConsulClient()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));
        agent.ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));
        agent.PassTTL(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        return (client, agent);
    }
}
