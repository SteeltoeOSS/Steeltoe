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
using Xunit;

namespace Steeltoe.Discovery.Consul.Client.Test
{
    public class ConsulClientFactoryTest
    {
        [Fact]
        public void CreateClient_ThrowsNullOptions()
        {
            Assert.Throws<ArgumentNullException>(() => ConsulClientFactory.CreateClient(null));
        }

        [Fact]
        public void CreateClient_Succeeds()
        {
            var opts = new ConsulOptions()
            {
                Host = "foobar",
                Datacenter = "datacenter",
                Token = "token",
                Username = "username",
                Password = "password",
                Port = 5555,
                Scheme = "https",
                WaitTime = "5s"
            };

            var client = ConsulClientFactory.CreateClient(opts) as ConsulClient;
            Assert.NotNull(client);
            Assert.NotNull(client.Config);
            Assert.Equal(opts.Datacenter, client.Config.Datacenter);
            Assert.Equal(opts.Token, client.Config.Token);
            Assert.Equal(opts.Host, client.Config.Address.Host);
            Assert.Equal(opts.Port, client.Config.Address.Port);
            Assert.Equal(opts.Scheme, client.Config.Address.Scheme);
            Assert.Equal(new TimeSpan(0, 0, 5), client.Config.WaitTime);
        }
    }
}
