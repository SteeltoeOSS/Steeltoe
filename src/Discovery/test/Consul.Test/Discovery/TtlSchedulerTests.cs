// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class TtlSchedulerTests
{
    [Fact]
    public void Add_Throws_Invalid_InstanceId()
    {
        var clientMoq = new Mock<IConsulClient>();
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);

        Assert.Throws<ArgumentException>(() => scheduler.Add(string.Empty));
    }

    [Fact]
    public void Add_DoesNothing_NoHeartbeatOptionsConfigured()
    {
        var clientMoq = new Mock<IConsulClient>();

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Heartbeat = null
        });

        var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        Assert.Empty(scheduler.ServiceHeartbeats);
    }

    [Fact]
    public async Task Add_AddsTimer()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions()
        });

        var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        Assert.NotEmpty(scheduler.ServiceHeartbeats);
        Assert.True(scheduler.ServiceHeartbeats.TryRemove("foobar", out PeriodicHeartbeat? heartbeat));
        Assert.NotNull(heartbeat);
        await heartbeat.DisposeAsync();
    }

    [Fact]
    public async Task Remove_Throws_Invalid_InstanceId()
    {
        var clientMoq = new Mock<IConsulClient>();
        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>();
        var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);

        await Assert.ThrowsAsync<ArgumentException>(async () => await scheduler.RemoveAsync(string.Empty));
    }

    [Fact]
    public async Task Remove_RemovesTimer()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions()
        });

        var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        Assert.NotEmpty(scheduler.ServiceHeartbeats);
        Assert.True(scheduler.ServiceHeartbeats.TryGetValue("foobar", out PeriodicHeartbeat? heartbeat));
        Assert.NotNull(heartbeat);

        await scheduler.RemoveAsync("foobar");
        Assert.False(scheduler.ServiceHeartbeats.TryGetValue("foobar", out _));
    }

    [Fact]
    public async Task Timer_CallsPassTTL()
    {
        var agentMoq = new Mock<IAgentEndpoint>();
        var clientMoq = new Mock<IConsulClient>();
        clientMoq.Setup(client => client.Agent).Returns(agentMoq.Object);

        var optionsMonitor = new TestOptionsMonitor<ConsulDiscoveryOptions>(new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions
            {
                TtlValue = 2
            }
        });

        var scheduler = new TtlScheduler(optionsMonitor, clientMoq.Object, NullLoggerFactory.Instance);
        scheduler.Add("foobar");

        await Task.Delay(2500);

        agentMoq.Verify(a => a.PassTTL("service:foobar", "ttl", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        await scheduler.RemoveAsync("foobar");
    }
}
