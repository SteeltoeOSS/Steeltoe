// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using FluentAssertions.Specialized;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul.Test.Registry;

public sealed class ConsulServiceRegistrarTest
{
    private static readonly WriteResult DefaultWriteResult = new();

    [Fact]
    public async Task WhenDisabled_NothingIsRegisteredOrDeregistered()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:enabled"] = "false"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        scope.Registrar.IsRunning.Should().BeFalse();
        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);
        await scope.Agent.DidNotReceive().ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());

        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await scope.Agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
        scope.LoggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task WhenRegisterIsFalse_NothingIsRegisteredOrDeregistered()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:register"] = "false"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        scope.Registrar.IsRunning.Should().BeTrue();
        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);
        await scope.Agent.DidNotReceive().ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());

        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await scope.Agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
        scope.LoggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Start_WithTtlCheck_RegistersAndStartsHeartbeat()
    {
        var appSettings = new Dictionary<string, string?>
        {
            // The shortest TTL that keeps the heartbeat interval at 1s, so the first heartbeat arrives quickly.
            ["consul:discovery:heartbeat:ttlValue"] = "2"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        var heartbeatSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        scope.Agent.PassTTL(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            heartbeatSent.TrySetResult();
            return Task.FromResult(DefaultWriteResult);
        });

        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        string instanceId = scope.Registrar.ServiceInstance.InstanceId;

        await scope.Agent.Received(1)
            .ServiceRegister(Arg.Is<AgentServiceRegistration>(payload => payload.ID == instanceId && payload.Check != null && payload.Check.TTL != null),
                Arg.Any<CancellationToken>());

        scope.Registrar.IsRunning.Should().BeTrue();
        scope.Registrar.Heartbeat.Should().NotBeNull();
        scope.Registrar.Heartbeat.CheckId.Should().Be($"service:{instanceId}");
        scope.Registrar.Heartbeat.Interval.Should().Be(TimeSpan.FromSeconds(1));
        scope.OptionsMonitor.ListenerCount.Should().Be(1);

        await heartbeatSent.Task.WaitAsync(TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken);
        await scope.Agent.Received().PassTTL($"service:{instanceId}", "ttl", Arg.Any<CancellationToken>());
        scope.LoggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Start_WithHttpCheck_RegistersWithoutHeartbeat()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:port"] = "8080",
            ["consul:discovery:heartbeat:enabled"] = "false"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        await scope.Agent.Received(1)
            .ServiceRegister(Arg.Is<AgentServiceRegistration>(payload => payload.Check != null && payload.Check.HTTP != null && payload.Check.TTL == null),
                Arg.Any<CancellationToken>());

        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);
        scope.LoggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Start_WithoutHealthCheck_RegistersWithoutCheckAndWithoutHeartbeat()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:registerHealthCheck"] = "false"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        await scope.Agent.Received(1).ServiceRegister(Arg.Is<AgentServiceRegistration>(payload => payload.Check == null), Arg.Any<CancellationToken>());

        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);
        scope.LoggerProvider.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Start_WhenHttpCheckIsConfiguredButPortIsUnknown_RegistersWithoutCheckAndLogsWarning()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:enabled"] = "false",
            ["consul:discovery:useAspNetCoreUrls"] = "false"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        await scope.Agent.Received(1).ServiceRegister(Arg.Is<AgentServiceRegistration>(payload => payload.Check == null), Arg.Any<CancellationToken>());

        IList<string> logMessages = scope.LoggerProvider.GetAll();
        logMessages.Should().ContainSingle();
        logMessages[0].Should().Be($"WARN {typeof(ConsulServiceRegistrar)}: Registering without an HTTP health check because the port of this app is unknown.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_WhenRegistrationFailsWithFailFast_ThrowsAndLeavesNothingBehind(bool isTimeout)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:failFast"] = "true"
        };

        Exception failure = CreateRegistrationFailure(isTimeout);

        await using var scope = new TestRegistrarScope(appSettings);
        scope.Agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns<Task<WriteResult>>(_ => throw failure);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        ExceptionAssertions<Exception> assertion = await action.Should().ThrowAsync<Exception>();
        assertion.Which.Should().BeSameAs(failure);

        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);

        string serviceId = scope.Registrar.ServiceInstance.ServiceId;
        IList<string> logMessages = scope.LoggerProvider.GetAll();
        logMessages.Should().ContainSingle();
        logMessages[0].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error registering service {serviceId} with Consul.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_WhenRegistrationFailsWithoutFailFast_LeavesNothingBehindAndDoesNotDeregister(bool isTimeout)
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:failFast"] = "false"
        };

        Exception failure = CreateRegistrationFailure(isTimeout);

        await using var scope = new TestRegistrarScope(appSettings);
        scope.Agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns<Task<WriteResult>>(_ => throw failure);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);

        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await scope.Agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());

        string serviceId = scope.Registrar.ServiceInstance.ServiceId;
        IList<string> logMessages = scope.LoggerProvider.GetAll();
        logMessages.Should().ContainSingle();
        logMessages[0].Should().Be($"WARN {typeof(ConsulServiceRegistrar)}: FailFast is false. Error registering service {serviceId} with Consul.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_WithRetry_SucceedsOnLaterAttempt(bool isTimeout)
    {
        Exception failure = CreateRegistrationFailure(isTimeout);

        await using var scope = new TestRegistrarScope(CreateRetrySettings());
        int attempts = 0;

        scope.Agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            attempts++;
            return attempts < 3 ? throw failure : Task.FromResult(DefaultWriteResult);
        });

        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        attempts.Should().Be(3);
        scope.Registrar.Heartbeat.Should().NotBeNull();

        string serviceId = scope.Registrar.ServiceInstance.ServiceId;
        IList<string> logMessages = scope.LoggerProvider.GetAll();
        logMessages.Should().HaveCount(4);
        logMessages[0].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error registering service {serviceId} with Consul.");
        logMessages[1].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Exception during 1 attempts of retryable action, will retry.");
        logMessages[2].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error registering service {serviceId} with Consul.");
        logMessages[3].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Exception during 2 attempts of retryable action, will retry.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_WithRetry_ThrowsAfterMaxAttempts(bool isTimeout)
    {
        Exception failure = CreateRegistrationFailure(isTimeout);

        await using var scope = new TestRegistrarScope(CreateRetrySettings());
        scope.Agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns<Task<WriteResult>>(_ => throw failure);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        ExceptionAssertions<Exception> assertion = await action.Should().ThrowAsync<Exception>();
        assertion.Which.Should().BeSameAs(failure);

        await scope.Agent.Received(3).ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
        scope.Registrar.Heartbeat.Should().BeNull();

        string serviceId = scope.Registrar.ServiceInstance.ServiceId;
        IList<string> logMessages = scope.LoggerProvider.GetAll();
        logMessages.Should().HaveCount(6);
        logMessages[0].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error registering service {serviceId} with Consul.");
        logMessages[1].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Exception during 1 attempts of retryable action, will retry.");
        logMessages[2].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error registering service {serviceId} with Consul.");
        logMessages[3].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Exception during 2 attempts of retryable action, will retry.");
        logMessages[4].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error registering service {serviceId} with Consul.");
        logMessages[5].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Exception during 3 attempts of retryable action, done with retries.");
    }

    [Fact]
    public async Task OptionsChange_WithAutoChangingInstanceId_UpdatesIntervalOfRunningHeartbeat()
    {
        var initialSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:ttlValue"] = "5"
        };

        await using var scope = new TestRegistrarScope(initialSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        PeriodicHeartbeat heartbeat = scope.Registrar.Heartbeat!;
        TimeSpan beforeInterval = heartbeat.Interval;

        var changedSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:ttlValue"] = "10"
        };

        // PostConfigure generates a new instance ID for every options build when none is configured, which must be ignored.
        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions(changedSettings);
        changedOptions.InstanceId.Should().NotBe(scope.OptionsMonitor.CurrentValue.InstanceId);

        scope.OptionsMonitor.Change(changedOptions);

        scope.Registrar.Heartbeat.Should().BeSameAs(heartbeat);
        heartbeat.Interval.Should().NotBe(beforeInterval);
        scope.OptionsMonitor.ListenerCount.Should().Be(1);
    }

    [Fact]
    public async Task Shutdown_ReleasesHeartbeatAndSubscription_AndDeregisters()
    {
        await using var scope = new TestRegistrarScope([]);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        scope.OptionsMonitor.ListenerCount.Should().Be(1);

        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        scope.Registrar.IsRunning.Should().BeFalse();
        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);
        await scope.Agent.Received(1).ServiceDeregister(scope.Registrar.ServiceInstance.InstanceId, Arg.Any<CancellationToken>());
        scope.LoggerProvider.GetAll().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Shutdown_WhenDeregistrationFails_LogsErrorAndStillReleasesResources(bool isTimeout)
    {
        Exception failure = CreateRegistrationFailure(isTimeout);

        await using var scope = new TestRegistrarScope([]);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        scope.Agent.ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Task<WriteResult>>(_ => throw failure);

        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        scope.Registrar.IsRunning.Should().BeFalse();
        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);

        string serviceId = scope.Registrar.ServiceInstance.ServiceId;
        IList<string> logMessages = scope.LoggerProvider.GetAll();
        logMessages.Should().ContainSingle();
        logMessages[0].Should().Be($"FAIL {typeof(ConsulServiceRegistrar)}: Error deregistering service {serviceId} with Consul.");
    }

    [Fact]
    public async Task Shutdown_DoesNotDeregister_WhenDeregisterIsFalse()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:deregister"] = "false"
        };

        await using var scope = new TestRegistrarScope(appSettings);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);
        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        scope.Registrar.Heartbeat.Should().BeNull();
        await scope.Agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shutdown_Deregisters_EvenWhenOptionsChangedToNoLongerRegister()
    {
        await using var scope = new TestRegistrarScope([]);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        var changedSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:register"] = "false"
        };

        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions(changedSettings);
        scope.OptionsMonitor.Change(changedOptions);
        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        await scope.Agent.Received(1).ServiceDeregister(scope.Registrar.ServiceInstance.InstanceId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Shutdown_DoesNotDeregisterButStillReleasesResources_WhenOptionsChangedToNotDeregister()
    {
        await using var scope = new TestRegistrarScope([]);
        await scope.Registrar.StartAsync(TestContext.Current.CancellationToken);

        var changedSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:deregister"] = "false"
        };

        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions(changedSettings);
        scope.OptionsMonitor.Change(changedOptions);

        await scope.Registrar.ShutdownAsync(TestContext.Current.CancellationToken);

        scope.Registrar.IsRunning.Should().BeFalse();
        scope.Registrar.Heartbeat.Should().BeNull();
        scope.OptionsMonitor.ListenerCount.Should().Be(0);
        await scope.Agent.DidNotReceive().ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static Exception CreateRegistrationFailure(bool isTimeout)
    {
        return isTimeout
            ? new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.", new TimeoutException())
            : new HttpRequestException("Connection refused.");
    }

    private static Dictionary<string, string?> CreateRetrySettings()
    {
        return new Dictionary<string, string?>
        {
            ["consul:discovery:failFast"] = "true",
            ["consul:discovery:retry:enabled"] = "true",
            ["consul:discovery:retry:initialInterval"] = "1",
            ["consul:discovery:retry:maxInterval"] = "1",
            ["consul:discovery:retry:maxAttempts"] = "3"
        };
    }

    private sealed class TestRegistrarScope : IAsyncDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public IAgentEndpoint Agent { get; }
        public TestOptionsMonitor<ConsulDiscoveryOptions> OptionsMonitor { get; }
        public CapturingLoggerProvider LoggerProvider { get; } = new();
        public ConsulServiceRegistrar Registrar { get; }

        public TestRegistrarScope(Dictionary<string, string?> appSettings)
        {
            Agent = Substitute.For<IAgentEndpoint>();
            Agent.ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));
            Agent.ServiceDeregister(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));
            Agent.PassTTL(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(DefaultWriteResult));

            var client = Substitute.For<IConsulClient>();
            client.Agent.Returns(Agent);

            OptionsMonitor = TestRegistrationFactory.CreateOptionsMonitor(appSettings);

            _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning).AddProvider(LoggerProvider));

            Registrar = new ConsulServiceRegistrar(client, OptionsMonitor, _loggerFactory);
        }

        public async ValueTask DisposeAsync()
        {
            await Registrar.DisposeAsync();
            _loggerFactory.Dispose();
        }
    }
}
