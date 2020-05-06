// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Consul;
using Moq;
using System;
using System.Threading;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class TtlSchedulerTests
    {
        [Fact]
        public void Add_Throws_Invalid_InstanceId()
        {
            var clientMoq = new Mock<IConsulClient>();
            var client = clientMoq.Object;
            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, client);
            Assert.Throws<ArgumentException>(() => sch.Add(string.Empty));
        }

        [Fact]
        public void Add_DoesNothing_NoHeartbeatOptionsConfigured()
        {
            var clientMoq = new Mock<IConsulClient>();
            var client = clientMoq.Object;
            var opts = new ConsulDiscoveryOptions() { Heartbeat = null };
            var sch = new TtlScheduler(opts, client);
            sch.Add("foobar");
            Assert.Empty(sch._serviceHeartbeats);
        }

        [Fact]
        public void Add_AddsTimer()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);
            var client = clientMoq.Object;
            var opts = new ConsulDiscoveryOptions()
            {
                Heartbeat = new ConsulHeartbeatOptions()
            };
            var sch = new TtlScheduler(opts, client);
            sch.Add("foobar");
            Assert.NotEmpty(sch._serviceHeartbeats);
            Assert.True(sch._serviceHeartbeats.TryRemove("foobar", out Timer timer));
            Assert.NotNull(timer);
            timer.Dispose();
        }

        [Fact]
        public void Remove_Throws_Invalid_InstanceId()
        {
            var clientMoq = new Mock<IConsulClient>();
            var client = clientMoq.Object;
            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, client);
            Assert.Throws<ArgumentException>(() => sch.Remove(string.Empty));
        }

        [Fact]
        public void Remove_Ignores_MissingInstanceId()
        {
            var clientMoq = new Mock<IConsulClient>();
            var client = clientMoq.Object;
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
            var client = clientMoq.Object;
            var opts = new ConsulDiscoveryOptions()
            {
                Heartbeat = new ConsulHeartbeatOptions()
            };
            var sch = new TtlScheduler(opts, client);
            sch.Add("foobar");
            Assert.NotEmpty(sch._serviceHeartbeats);
            Assert.True(sch._serviceHeartbeats.TryGetValue("foobar", out Timer timer));
            Assert.NotNull(timer);
            sch.Remove("foobar");
            Assert.False(sch._serviceHeartbeats.TryGetValue("foobar", out Timer timer2));
        }

        [Fact]
        public void Timer_CallsPassTtl()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();
            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);
            var client = clientMoq.Object;
            var opts = new ConsulDiscoveryOptions()
            {
                Heartbeat = new ConsulHeartbeatOptions() { TtlValue = 2 }
            };
            var sch = new TtlScheduler(opts, client);
            sch.Add("foobar");
            Thread.Sleep(2500);
            agentMoq.Verify(a => a.PassTTL("service:foobar", "ttl", default(CancellationToken)), Times.AtLeastOnce);
            sch.Remove("foobar");
        }
    }
}
