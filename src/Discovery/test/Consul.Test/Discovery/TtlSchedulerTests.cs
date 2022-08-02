// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Moq;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test;

public class TtlSchedulerTests
{
    [Fact]
    public void Add_Throws_Invalid_InstanceId()
    {
        var clientMoq = new Mock<IConsulClient>();
        IConsulClient client = clientMoq.Object;
        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, client);
        Assert.Throws<ArgumentException>(() => sch.Add(string.Empty));
    }

    [Fact]
    public void Add_DoesNothing_NoHeartbeatOptionsConfigured()
    {
        var clientMoq = new Mock<IConsulClient>();
        IConsulClient client = clientMoq.Object;

        var opts = new ConsulDiscoveryOptions
        {
            Heartbeat = null
        };

        var sch = new TtlScheduler(opts, client);
        sch.Add("foobar");
        Assert.Empty(sch.ServiceHeartbeats);
    }

    [Fact]
    public void Add_AddsTimer()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);
        IConsulClient client = clientMoq.Object;

        var opts = new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions()
        };

        var sch = new TtlScheduler(opts, client);
        sch.Add("foobar");
        Assert.NotEmpty(sch.ServiceHeartbeats);
        Assert.True(sch.ServiceHeartbeats.TryRemove("foobar", out Timer timer));
        Assert.NotNull(timer);
        timer.Dispose();
    }

    [Fact]
    public void Remove_Throws_Invalid_InstanceId()
    {
        var clientMoq = new Mock<IConsulClient>();
        IConsulClient client = clientMoq.Object;
        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, client);
        Assert.Throws<ArgumentException>(() => sch.Remove(string.Empty));
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void Remove_Ignores_MissingInstanceId()
#pragma warning restore S2699 // Tests should include assertions
    {
        var clientMoq = new Mock<IConsulClient>();
        IConsulClient client = clientMoq.Object;
        var opts = new ConsulDiscoveryOptions();
        var sch = new TtlScheduler(opts, client);
        sch.Remove("barfoo");
    }

    [Fact]
    public void Remove_RemovesTimer()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();

        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);
        IConsulClient client = clientMoq.Object;

        var opts = new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions()
        };

        var sch = new TtlScheduler(opts, client);
        sch.Add("foobar");
        Assert.NotEmpty(sch.ServiceHeartbeats);
        Assert.True(sch.ServiceHeartbeats.TryGetValue("foobar", out Timer timer));
        Assert.NotNull(timer);
        sch.Remove("foobar");
        Assert.False(sch.ServiceHeartbeats.TryGetValue("foobar", out _));
    }

    [Fact]
    public void Timer_CallsPassTtl()
    {
        var clientMoq = new Mock<IConsulClient>();
        var agentMoq = new Mock<IAgentEndpoint>();
        clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);
        IConsulClient client = clientMoq.Object;

        var opts = new ConsulDiscoveryOptions
        {
            Heartbeat = new ConsulHeartbeatOptions
            {
                TtlValue = 2
            }
        };

        var sch = new TtlScheduler(opts, client);
        sch.Add("foobar");
        Thread.Sleep(2500);
        agentMoq.Verify(a => a.PassTTL("service:foobar", "ttl", default), Times.AtLeastOnce);
        sch.Remove("foobar");
    }
}
