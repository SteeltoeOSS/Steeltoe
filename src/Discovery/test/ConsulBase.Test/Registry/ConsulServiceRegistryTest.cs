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
using Microsoft.Extensions.Configuration;
using Moq;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Discovery;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Discovery.Consul.Registry.Test
{
    public class ConsulServiceRegistryTest
    {
        [Fact]
        public void Construtor_ThrowsOnNulls()
        {
            var clientMoq = new Mock<IConsulClient>();
            Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistry(null, new ConsulDiscoveryOptions()));
            Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistry(clientMoq.Object, (ConsulDiscoveryOptions)null));
        }

        [Fact]
        public void RegisterAsync_ThrowsOnNull()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            Assert.ThrowsAsync<ArgumentNullException>(() => reg.RegisterAsync(null));
        }

        [Fact]
        public async void RegisterAsync_CallsServiceRegister_AddsHeartbeatToScheduler()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
            {
                    { "spring:application:name", "foobar" }
            });
            var config = builder.Build();
            var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(config));
            await reg.RegisterAsync(registration);

            agentMoq.Verify(a => a.ServiceRegister(registration.Service, default), Times.Once);

            Assert.Single(sch._serviceHeartbeats);
            Assert.Contains(registration.InstanceId, sch._serviceHeartbeats.Keys);
            sch.Remove(registration.InstanceId);
        }

        [Fact]
        public void DeegisterAsync_ThrowsOnNull()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            Assert.ThrowsAsync<ArgumentNullException>(() => reg.DeregisterAsync(null));
        }

        [Fact]
        public async void DeregisterAsync_CallsServiceDeregister_RemovesHeartbeatFromScheduler()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
            {
                    { "spring:application:name", "foobar" }
            });
            var config = builder.Build();
            var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(config));
            await reg.RegisterAsync(registration);

            agentMoq.Verify(a => a.ServiceRegister(registration.Service, default), Times.Once);

            Assert.Single(sch._serviceHeartbeats);
            Assert.Contains(registration.InstanceId, sch._serviceHeartbeats.Keys);

            await reg.DeregisterAsync(registration);
            agentMoq.Verify(a => a.ServiceDeregister(registration.Service.ID, default), Times.Once);
            Assert.Empty(sch._serviceHeartbeats);
        }

        [Fact]
        public void SetStatusAsync_ThrowsOnNull()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            Assert.ThrowsAsync<ArgumentNullException>(() => reg.SetStatusAsync(null, string.Empty));
        }

        [Fact]
        public void SetStatusAsync_ThrowsInvalidStatus()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "foobar" }
            });
            var config = builder.Build();
            var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(config));

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            Assert.ThrowsAsync<ArgumentException>(() => reg.SetStatusAsync(registration, string.Empty));
        }

        [Fact]
        public async void SetStatusAsync_CallsConsulClient()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "foobar" }
            });
            var config = builder.Build();
            var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(config));

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            await reg.SetStatusAsync(registration, "Up");
            agentMoq.Verify(a => a.DisableServiceMaintenance(registration.InstanceId, default), Times.Once);
            await reg.SetStatusAsync(registration, "Out_of_Service");
            agentMoq.Verify(a => a.EnableServiceMaintenance(registration.InstanceId, "OUT_OF_SERVICE", default), Times.Once);
        }

        [Fact]
        public void GetStatusAsync_ThrowsOnNull()
        {
            var clientMoq = new Mock<IConsulClient>();
            var agentMoq = new Mock<IAgentEndpoint>();

            clientMoq.Setup(c => c.Agent).Returns(agentMoq.Object);

            var opts = new ConsulDiscoveryOptions();
            var sch = new TtlScheduler(opts, clientMoq.Object);

            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);
            Assert.ThrowsAsync<ArgumentNullException>(() => reg.GetStatusAsync(null));
        }

        [Fact]
        public async void GetStatusAsync_ReturnsExpected()
        {
            var opts = new ConsulDiscoveryOptions();
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "spring:application:name", "foobar" }
                });
            var config = builder.Build();
            var registration = ConsulRegistration.CreateRegistration(opts, new ApplicationInstanceInfo(config));

            var queryResult = new QueryResult<HealthCheck[]>()
            {
                Response = new HealthCheck[]
                {
                    new HealthCheck()
                    {
                        ServiceID = registration.InstanceId,
                        Name = "Service Maintenance Mode"
                    },
                    new HealthCheck()
                    {
                        ServiceID = "foobar",
                        Name = "Service Maintenance Mode"
                    }
                }
            };
            var result = Task.FromResult(queryResult);

            var clientMoq = new Mock<IConsulClient>();
            var healthMoq = new Mock<IHealthEndpoint>();

            clientMoq.Setup(c => c.Health).Returns(healthMoq.Object);
            healthMoq.Setup(h => h.Checks(registration.ServiceId, QueryOptions.Default, default)).Returns(result);

            var sch = new TtlScheduler(opts, clientMoq.Object);
            var reg = new ConsulServiceRegistry(clientMoq.Object, opts, sch);

            var ret = await reg.GetStatusAsync(registration);
            Assert.Equal("OUT_OF_SERVICE", ret);
        }
    }
}
