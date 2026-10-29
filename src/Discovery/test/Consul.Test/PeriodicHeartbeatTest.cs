// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Steeltoe.Discovery.Consul.Test;

public sealed class PeriodicHeartbeatTest
{
    [Fact]
    public async Task Start_SendsPassTtlForConfiguredService()
    {
        using var heartbeatSent = new SemaphoreSlim(0);
        var agent = Substitute.For<IAgentEndpoint>();

        // ReSharper disable once AccessToDisposedClosure
        agent.PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            heartbeatSent.Release();
            return Task.FromResult(new WriteResult());
        });

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var timeProvider = new FakeTimeProvider();

        // The interval is longer than the timeouts below, so a heartbeat can only arrive by advancing the fake clock.
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(90);
        TimeSpan interval = optionsMonitor.CurrentValue.Heartbeat!.ComputeHeartbeatInterval();

        await using var heartbeat =
            new PeriodicHeartbeat("service:foobar", interval, optionsMonitor, client, timeProvider, NullLogger<PeriodicHeartbeat>.Instance);

        heartbeat.Start();

        // The heartbeats are sent on another thread. The timeout is only a safety net against hanging forever.
        timeProvider.Advance(interval);
        (await heartbeatSent.WaitAsync(30.Seconds(), TestContext.Current.CancellationToken)).Should().BeTrue();

        timeProvider.Advance(interval);
        (await heartbeatSent.WaitAsync(30.Seconds(), TestContext.Current.CancellationToken)).Should().BeTrue();

        await agent.Received(2).PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_heartbeat_does_not_stop_sending_heartbeats()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information).AddProvider(loggerProvider));

        using var heartbeatAttempted = new SemaphoreSlim(0);
        int attemptCount = 0;
        var agent = Substitute.For<IAgentEndpoint>();

        // ReSharper disable once AccessToDisposedClosure
        agent.PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            int attempt = Interlocked.Increment(ref attemptCount);
            heartbeatAttempted.Release();

            return attempt == 1 ? throw new HttpRequestException("Connection refused.") : Task.FromResult(new WriteResult());
        });

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var timeProvider = new FakeTimeProvider();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(90);
        TimeSpan interval = optionsMonitor.CurrentValue.Heartbeat!.ComputeHeartbeatInterval();
        ILogger<PeriodicHeartbeat> logger = loggerFactory.CreateLogger<PeriodicHeartbeat>();

        await using var heartbeat = new PeriodicHeartbeat("service:foobar", interval, optionsMonitor, client, timeProvider, logger);

        heartbeat.Start();

        timeProvider.Advance(interval);
        (await heartbeatAttempted.WaitAsync(30.Seconds(), TestContext.Current.CancellationToken)).Should().BeTrue();

        // The next heartbeat is sent even though the previous one failed.
        timeProvider.Advance(interval);
        (await heartbeatAttempted.WaitAsync(30.Seconds(), TestContext.Current.CancellationToken)).Should().BeTrue();

        loggerProvider.GetAsText().Should().Contain("Failed to send Consul heartbeat for 'service:foobar'");
    }

    [Fact]
    public async Task OptionsChange_BeforeStart_IsApplied()
    {
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(30);
        await using PeriodicHeartbeat heartbeat = CreateHeartbeat(optionsMonitor);

        ConsulDiscoveryOptions changedOptions = CreateOptions(60);
        optionsMonitor.Change(changedOptions);

        heartbeat.Start();

        heartbeat.Interval.Should().Be(changedOptions.Heartbeat!.ComputeHeartbeatInterval());
    }

    [Fact]
    public async Task OptionsChange_WhileRunning_IsApplied()
    {
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(30);
        await using PeriodicHeartbeat heartbeat = CreateHeartbeat(optionsMonitor);

        heartbeat.Start();

        ConsulDiscoveryOptions changedOptions = CreateOptions(60);
        optionsMonitor.Change(changedOptions);

        heartbeat.Interval.Should().Be(changedOptions.Heartbeat!.ComputeHeartbeatInterval());
    }

    [Fact]
    public async Task OptionsChange_WithUnchangedInterval_DoesNotPostponeNextHeartbeat()
    {
        using var heartbeatSent = new SemaphoreSlim(0);
        var agent = Substitute.For<IAgentEndpoint>();

        // ReSharper disable once AccessToDisposedClosure
        agent.PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            heartbeatSent.Release();
            return Task.FromResult(new WriteResult());
        });

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var timeProvider = new FakeTimeProvider();
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(90);
        TimeSpan interval = optionsMonitor.CurrentValue.Heartbeat!.ComputeHeartbeatInterval();

        await using var heartbeat =
            new PeriodicHeartbeat("service:foobar", interval, optionsMonitor, client, timeProvider, NullLogger<PeriodicHeartbeat>.Instance);

        heartbeat.Start();
        timeProvider.Advance(interval - 1.Seconds());

        // Every options reload raises a change, also when unrelated to the heartbeat. Applying an unchanged interval would restart the countdown,
        // so reloads that are more frequent than the interval would postpone the heartbeat forever.
        optionsMonitor.Change(CreateOptions(90));

        timeProvider.Advance(1.Seconds());

        (await heartbeatSent.WaitAsync(30.Seconds(), TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_StopsListeningAndCanBeCalledTwice()
    {
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(30);
        PeriodicHeartbeat heartbeat = CreateHeartbeat(optionsMonitor);
        heartbeat.Start();

        optionsMonitor.ListenerCount.Should().Be(1);

        await heartbeat.DisposeAsync();
        await heartbeat.DisposeAsync();

        optionsMonitor.ListenerCount.Should().Be(0);
    }

    [Fact]
    public async Task OptionsChange_ArrivingAfterDispose_IsIgnored()
    {
        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(30);

        using var blockerEntered = new ManualResetEventSlim();
        using var blockerRelease = new ManualResetEventSlim();

        // Registered first, so it holds up the notification while the heartbeat is disposed. The monitor has already taken its snapshot
        // of listeners by then, so the heartbeat still receives the change after it was disposed.
        optionsMonitor.OnChange((_, _) =>
        {
            // ReSharper disable AccessToDisposedClosure
            blockerEntered.Set();
            blockerRelease.Wait(TestContext.Current.CancellationToken);
            // ReSharper restore AccessToDisposedClosure
        });

        PeriodicHeartbeat heartbeat = CreateHeartbeat(optionsMonitor);
        heartbeat.Start();
        TimeSpan before = heartbeat.Interval;

        Task change = Task.Run(() => optionsMonitor.Change(CreateOptions(60)), TestContext.Current.CancellationToken);
        blockerEntered.Wait(TestContext.Current.CancellationToken);

        await heartbeat.DisposeAsync();
        blockerRelease.Set();

        // Setting the period of the disposed timer would throw ObjectDisposedException on the thread that raises the change.
        await change;

        heartbeat.Interval.Should().Be(before);
    }

    private static PeriodicHeartbeat CreateHeartbeat(TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor)
    {
        var client = Substitute.For<IConsulClient>();
        var agent = Substitute.For<IAgentEndpoint>();
        agent.PassTTL(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new WriteResult()));
        client.Agent.Returns(agent);

        TimeSpan interval = optionsMonitor.CurrentValue.Heartbeat!.ComputeHeartbeatInterval();
        var timeProvider = new FakeTimeProvider();
        return new PeriodicHeartbeat("service:foobar", interval, optionsMonitor, client, timeProvider, NullLogger<PeriodicHeartbeat>.Instance);
    }

    private static TestOptionsMonitor<ConsulDiscoveryOptions> CreateOptionsMonitor(int ttlValue)
    {
        ConsulDiscoveryOptions options = CreateOptions(ttlValue);
        return TestOptionsMonitor.Create(options);
    }

    private static ConsulDiscoveryOptions CreateOptions(int ttlValue)
    {
        return new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions
            {
                TtlValue = ttlValue
            }
        };
    }
}
