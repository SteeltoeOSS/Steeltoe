// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        var heartbeatSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var agent = Substitute.For<IAgentEndpoint>();

        agent.PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            heartbeatSent.TrySetResult();
            return Task.FromResult(new WriteResult());
        });

        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(2);
        await using var heartbeat = new PeriodicHeartbeat("service:foobar", 1.Seconds(), optionsMonitor, client, NullLogger<PeriodicHeartbeat>.Instance);

        heartbeat.Start();

        // The timeout is only a safety net against hanging forever. Under CI pressure the tick may take much longer than the interval.
        await heartbeatSent.Task.WaitAsync(2.Minutes(), TestContext.Current.CancellationToken);

        heartbeatSent.Task.IsCompletedSuccessfully.Should().BeTrue();
        await agent.Received().PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>());
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
    public async Task OptionsChange_WithUnchangedInterval_IsNotApplied()
    {
        var loggerProvider = new CapturingLoggerProvider();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug).AddProvider(loggerProvider));

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = CreateOptionsMonitor(30);
        TimeSpan interval = optionsMonitor.CurrentValue.Heartbeat!.ComputeHeartbeatInterval();

        ILogger<PeriodicHeartbeat> logger = loggerFactory.CreateLogger<PeriodicHeartbeat>();
        await using var heartbeat = new PeriodicHeartbeat("service:foobar", interval, optionsMonitor, Substitute.For<IConsulClient>(), logger);

        heartbeat.Start();

        // Setting the timer period restarts its countdown, even to the same value. Every options reload raises a change, also when unrelated to the
        // heartbeat, so unchanged intervals must not be applied (or reloads more frequent than the interval would postpone heartbeats forever).
        optionsMonitor.Change(CreateOptions(30));

        loggerProvider.GetAsText().Should().NotContain("interval for 'service:foobar' changed");

        optionsMonitor.Change(CreateOptions(60));

        loggerProvider.GetAsText().Should().Contain("interval for 'service:foobar' changed");
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
        return new PeriodicHeartbeat("service:foobar", interval, optionsMonitor, client, NullLogger<PeriodicHeartbeat>.Instance);
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
