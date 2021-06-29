// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Connector.Services.Test
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
            Assert.True(factory.Accepts(s));
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
            Assert.False(factory.Accepts(s));
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
