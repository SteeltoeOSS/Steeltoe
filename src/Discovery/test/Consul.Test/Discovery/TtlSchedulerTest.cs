// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class TtlSchedulerTest
{
    [Fact]
    public async Task Add_Throws_Invalid_InstanceId()
    {
        var client = Substitute.For<IConsulClient>();
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);

        // ReSharper disable once AccessToDisposedClosure
        Action action = () => scheduler.Add(string.Empty);

        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public async Task Add_DoesNothing_NoHeartbeatOptionsConfigured()
    {
        var client = Substitute.For<IConsulClient>();

        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Heartbeat = null
        });

        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        scheduler.ServiceHeartbeats.Should().BeEmpty();
    }

    [Fact]
    public async Task Add_AddsTimer()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();

        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        scheduler.ServiceHeartbeats.Should().NotBeEmpty();
        scheduler.ServiceHeartbeats.TryRemove("foobar", out PeriodicHeartbeat? heartbeat).Should().BeTrue();
        heartbeat.Should().NotBeNull();
    }

    [Fact]
    public async Task Can_Change_Timer_Interval()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var options = new ConsulDiscoveryOptions
        {
            InstanceId = "foobar",
            Heartbeat = new ConsulHeartbeatOptions
            {
                TtlValue = 5
            }
        };

        TestOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);

        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        scheduler.Add(options.InstanceId);

        PeriodicHeartbeat heartbeat = scheduler.ServiceHeartbeats.Should().ContainKey(options.InstanceId).WhoseValue;
        TimeSpan beforeInterval = heartbeat.Interval;

        options.Heartbeat.TtlValue = 10;
        optionsMonitor.Change(options);

        TimeSpan afterInterval = heartbeat.Interval;
        afterInterval.Should().NotBe(beforeInterval);
    }

    [Fact]
    public async Task Remove_Throws_Invalid_InstanceId()
    {
        var client = Substitute.For<IConsulClient>();
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);

        // ReSharper disable once AccessToDisposedClosure
        Func<Task> action = async () => await scheduler.RemoveAsync(string.Empty);

        await action.Should().ThrowExactlyAsync<ArgumentException>();
    }

    [Fact]
    public async Task Remove_RemovesTimer()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        scheduler.ServiceHeartbeats.Should().NotBeEmpty();
        scheduler.ServiceHeartbeats.TryGetValue("foobar", out PeriodicHeartbeat? heartbeat).Should().BeTrue();
        heartbeat.Should().NotBeNull();

        await scheduler.RemoveAsync("foobar");
        scheduler.ServiceHeartbeats.TryGetValue("foobar", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Timer_CallsPassTTL()
    {
        var agent = Substitute.For<IAgentEndpoint>();
        var client = Substitute.For<IConsulClient>();
        client.Agent.Returns(agent);

        var optionsMonitor = TestOptionsMonitor.Create(new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions
            {
                TtlValue = 2
            }
        });

        await using var scheduler = new TtlScheduler(optionsMonitor, client, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        await Task.Delay(2500.Milliseconds(), TestContext.Current.CancellationToken);

        await agent.Received().PassTTL("service:foobar", "ttl", Arg.Any<CancellationToken>());
        await scheduler.RemoveAsync("foobar");
    }
}
