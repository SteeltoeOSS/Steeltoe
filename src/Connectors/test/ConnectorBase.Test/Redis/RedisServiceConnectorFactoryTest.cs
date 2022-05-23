// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Redis.Test
{
    [Collection("Redis")]
    public class RedisServiceConnectorFactoryTest
    {
        [Fact]
        public void Create_CanReturnRedisCache()
        {
            var config = new RedisCacheConnectorOptions
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceName = "instanceId"
            };
            var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");

            var factory = new RedisServiceConnectorFactory(si, config, typeof(RedisCache), typeof(RedisCacheOptions), null);
            var cache = factory.Create(null);

            Assert.NotNull(cache);
            Assert.IsType<RedisCache>(cache);
        }

        [Fact]
        public void Create_CanReturnConnectionMultiplexer()
        {
            var config = new RedisCacheConnectorOptions
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceName = "instanceId",
                AbortOnConnectFail = false,
                ConnectTimeout = 1
            };
            var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "127.0.0.1", 4321, "sipassword");

            var factory = new RedisServiceConnectorFactory(si, config, typeof(ConnectionMultiplexer), typeof(ConfigurationOptions), RedisTypeLocator.StackExchangeInitializer);
            var multi = factory.Create(null);

            Assert.NotNull(multi);
            Assert.IsType<ConnectionMultiplexer>(multi);
        }
    }
}
