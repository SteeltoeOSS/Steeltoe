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
using Steeltoe.Discovery.Consul.Registry;
using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ThisServiceInstanceTest
    {
        [Fact]
        public void Constructor_Initializes()
        {
            var serviceRegistration = new AgentServiceRegistration()
            {
                ID = "ID",
                Name = "foobar",
                Address = "test.foo.bar",
                Port = 1234,
                Tags = new string[] { "foo=bar" }
            };
            var opts = new ConsulDiscoveryOptions();
            var registration = new ConsulRegistration(serviceRegistration, opts);
            var thiz = new ThisServiceInstance(registration);
            Assert.Equal("test.foo.bar", thiz.Host);
            Assert.Equal("foobar", thiz.ServiceId);
            Assert.False(thiz.IsSecure);
            Assert.Equal(1234, thiz.Port);
            Assert.Single(thiz.Metadata);
            Assert.Contains("foo", thiz.Metadata.Keys);
            Assert.Contains("bar", thiz.Metadata.Values);
            Assert.Equal(new Uri("http://test.foo.bar:1234"), thiz.Uri);
        }
    }
}
