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

using Moq;
using Steeltoe.Discovery.Consul.Discovery;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Discovery.Consul.Registry.Test
{
    public class ConsulServiceRegistrarTest
    {
        [Fact]
        public void Construtor_ThrowsOnNulls()
        {
            var registry = new Mock<IConsulServiceRegistry>().Object;
            var options = new ConsulDiscoveryOptions();
            var registration = new ConsulRegistration();

            Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistrar(null, options, registration));
            Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistrar(registry, (ConsulDiscoveryOptions)null, registration));
            Assert.Throws<ArgumentNullException>(() => new ConsulServiceRegistrar(registry, options, null));
        }

        [Fact]
        public void Register_CallsRegistry()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions();
            var registration = new ConsulRegistration();

            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Register();
            regMoq.Verify(a => a.Register(registration), Times.Once);
        }

        [Fact]
        public void Deregister_CallsRegistry()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions();
            var registration = new ConsulRegistration();

            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Deregister();
            regMoq.Verify(a => a.Deregister(registration), Times.Once);
        }

        [Fact]
        public void Register_DoesNotCallRegistry()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions()
            {
                Register = false
            };
            var registration = new ConsulRegistration();

            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Register();
            regMoq.Verify(a => a.Register(registration), Times.Never);
        }

        [Fact]
        public void Deregister_DoesNotCallRegistry()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions()
            {
                Deregister = false
            };
            var registration = new ConsulRegistration();

            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Deregister();
            regMoq.Verify(a => a.Deregister(registration), Times.Never);
        }

        [Fact]
        public void Start_DoesNotStart()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions()
            {
                Enabled = false
            };
            var registration = new ConsulRegistration();
            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Start();
            Assert.Equal(0, reg._running);
        }

        [Fact]
        public void Start_CallsRegistry()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions();
            var registration = new ConsulRegistration();
            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Start();
            Assert.Equal(1, reg._running);
            regMoq.Verify(a => a.Register(registration), Times.Once);
        }

        [Fact]
        public void Dispose_CallsRegistry()
        {
            var regMoq = new Mock<IConsulServiceRegistry>();
            var options = new ConsulDiscoveryOptions();
            var registration = new ConsulRegistration();
            var reg = new ConsulServiceRegistrar(regMoq.Object, options, registration);
            reg.Start();
            Assert.Equal(1, reg._running);
            regMoq.Verify(a => a.Register(registration), Times.Once);
            reg.Dispose();
            regMoq.Verify(a => a.Deregister(registration), Times.Once);
            regMoq.Verify(a => a.Dispose(), Times.Once);
            Assert.Equal(0, reg._running);
        }
    }
}
