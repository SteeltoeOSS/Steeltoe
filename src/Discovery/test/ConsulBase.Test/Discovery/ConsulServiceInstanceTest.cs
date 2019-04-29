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
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulServiceInstanceTest
    {
        [Fact]
        public void Constructor_Initializes()
        {
            var healthService = new ServiceEntry()
            {
                Service = new AgentService()
                {
                    Service = "ServiceId",
                    Address = "foo.bar.com",
                    Port = 1234,
                    Tags = new string[] { "foo=bar", "secure=true" }
                }
            };

            var si = new ConsulServiceInstance(healthService);
            Assert.Equal("foo.bar.com", si.Host);
            Assert.Equal("ServiceId", si.ServiceId);
            Assert.True(si.IsSecure);
            Assert.Equal(1234, si.Port);
            Assert.Equal(2, si.Metadata.Count);
            Assert.Contains("foo", si.Metadata.Keys);
            Assert.Contains("secure", si.Metadata.Keys);
            Assert.Contains("bar", si.Metadata.Values);
            Assert.Contains("true", si.Metadata.Values);
            Assert.Equal(new Uri("https://foo.bar.com:1234"), si.Uri);
        }
    }
}
