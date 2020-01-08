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

using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class RedisServiceInfoFactoryTest
    {
        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-redis",
                Tags = new string[] { "redis", "pivotal" },
                Name = "myRedisService",
                Plan = "shared-vm",
                Credentials = new Credential()
                {
                    { "host", new Credential("192.168.0.103") },
                    { "password", new Credential("133de7c8-9f3a-4df1-8a10-676ba7ddaa10") },
                    { "port", new Credential("60287") }
                }
            };
            var factory = new RedisServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsInvalidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-redis",
                Tags = new string[] { "foobar", "pivotal" },
                Name = "myRedisService",
                Plan = "shared-vm",
                Credentials = new Credential()
                {
                    { "host", new Credential("192.168.0.103") },
                    { "password", new Credential("133de7c8-9f3a-4df1-8a10-676ba7ddaa10") },
                    { "port", new Credential("60287") }
                }
            };
            var factory = new RedisServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-redis",
                Tags = new string[] { "redis", "pivotal" },
                Name = "myRedisService",
                Plan = "shared-vm",
                Credentials = new Credential()
                {
                    { "host", new Credential("192.168.0.103") },
                    { "password", new Credential("133de7c8-9f3a-4df1-8a10-676ba7ddaa10") },
                    { "port", new Credential("60287") }
                }
            };
            var factory = new RedisServiceInfoFactory();
            var info = factory.Create(s) as RedisServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("myRedisService", info.Id);
            Assert.Equal("133de7c8-9f3a-4df1-8a10-676ba7ddaa10", info.Password);
            Assert.Equal("192.168.0.103", info.Host);
            Assert.Equal(60287, info.Port);
            Assert.Equal("redis", info.Scheme);
        }

        [Fact]
        public void Create_CreatesValidServiceBindingForTLS()
        {
            var s = new Service()
            {
                Label = "p.redis",
                Tags = new string[] { "redis", "pivotal" },
                Name = "myRedisService",
                Plan = "cache-small",
                Credentials = new Credential()
                {
                    { "host", new Credential("192.168.0.103") },
                    { "password", new Credential("133de7c8-9f3a-4df1-8a10-676ba7ddaa10") },
                    { "port", new Credential("60287") },
                    { "tls_port", new Credential("6287") }
                }
            };

            var factory = new RedisServiceInfoFactory();
            var info = factory.Create(s) as RedisServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("myRedisService", info.Id);
            Assert.Equal("133de7c8-9f3a-4df1-8a10-676ba7ddaa10", info.Password);
            Assert.Equal("192.168.0.103", info.Host);
            Assert.Equal(6287, info.Port);
            Assert.Equal("rediss", info.Scheme);
        }
    }
}
