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
        await using var scope = new TestRegistrarScope([]);
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
        scope.Registrar.Heartbeat.Interval.Should().Be(TimeSpan.FromSeconds(20));
        scope.OptionsMonitor.ListenerCount.Should().Be(1);

        // The heartbeat is sent on another thread. The timeout is only a safety net against hanging forever.
        scope.TimeProvider.Advance(scope.Registrar.Heartbeat.Interval);
        await heartbeatSent.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
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

        Task start = scope.Registrar.StartAsync(TestContext.Current.CancellationToken);
        await AdvanceThroughRetryDelaysAsync(scope, 2);
        await start;

        attempts.Should().Be(3);
        scope.Registrar.Heartbeat.Should().NotBeNull();

        // The delay doubles, but is limited to the maximum. The timer after those is the heartbeat.
        scope.TimeProvider.DueTimes.Take(2).Select(delay => (int)delay.TotalMilliseconds).Should().Equal(100, 150);

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

        Task start = scope.Registrar.StartAsync(TestContext.Current.CancellationToken);
        await AdvanceThroughRetryDelaysAsync(scope, 2);

        Func<Task> action = async () => await start;

        ExceptionAssertions<Exception> assertion = await action.Should().ThrowAsync<Exception>();
        assertion.Which.Should().BeSameAs(failure);

        await scope.Agent.Received(3).ServiceRegister(Arg.Any<AgentServiceRegistration>(), Arg.Any<CancellationToken>());
        scope.Registrar.Heartbeat.Should().BeNull();

        // The delay doubles, but is limited to the maximum.
        scope.TimeProvider.DueTimes.Select(delay => (int)delay.TotalMilliseconds).Should().Equal(100, 150);

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
        heartbeat.Interval.Should().BeCloseTo(TimeSpan.FromSeconds(3.33), TimeSpan.FromMilliseconds(10));

        var changedSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:heartbeat:ttlValue"] = "10"
        };

        // PostConfigure generates a new instance ID for every options build when none is configured, which must be ignored.
        ConsulDiscoveryOptions changedOptions = TestRegistrationFactory.CreateDiscoveryOptions(changedSettings);
        changedOptions.InstanceId.Should().NotBe(scope.OptionsMonitor.CurrentValue.InstanceId);

        scope.OptionsMonitor.Change(changedOptions);

        scope.Registrar.Heartbeat.Should().BeSameAs(heartbeat);
        heartbeat.Interval.Should().BeCloseTo(TimeSpan.FromSeconds(6.67), TimeSpan.FromMilliseconds(10));
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
            ["consul:discovery:retry:initialInterval"] = "100",
            ["consul:discovery:retry:multiplier"] = "2",
            ["consul:discovery:retry:maxInterval"] = "150",
            ["consul:discovery:retry:maxAttempts"] = "3"
        };
    }

    private static async Task AdvanceThroughRetryDelaysAsync(TestRegistrarScope scope, int delayCount)
    {
        // Each retry waits first. Advancing the clock by exactly that delay lets the next attempt start.
        for (int index = 0; index < delayCount; index++)
        {
            TimeSpan delay = await scope.TimeProvider.WaitForNextTimerAsync(TestContext.Current.CancellationToken);
            scope.TimeProvider.Advance(delay);
        }
    }

    private sealed class TestRegistrarScope : IAsyncDisposable
    {
        private readonly ILoggerFactory _loggerFactory;

        public IAgentEndpoint Agent { get; }
        public TestOptionsMonitor<ConsulDiscoveryOptions> OptionsMonitor { get; }
        public CapturingLoggerProvider LoggerProvider { get; } = new();
        public RecordingFakeTimeProvider TimeProvider { get; } = new();
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
            Registrar = new ConsulServiceRegistrar(client, OptionsMonitor, TimeProvider, _loggerFactory);
        }

        public async ValueTask DisposeAsync()
        {
            await Registrar.DisposeAsync();
            _loggerFactory.Dispose();
        }
    }
}
