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

using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Steeltoe.CloudFoundry.Connector.App;
using Steeltoe.CloudFoundry.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Redis.Test
{
    [Collection("Redis")]
    public class RedisServiceConnectorFactoryTest
    {
        [Fact]
        public void Create_CanReturnRedisCache()
        {
            // arrange
            RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceName = "instanceId"
            };
            RedisServiceInfo si = new RedisServiceInfo("myId", "foobar", 4321, "sipassword")
            {
                ApplicationInfo = new ApplicationInstanceInfo()
                {
                    InstanceId = "instanceId"
                }
            };

            // act
            var factory = new RedisServiceConnectorFactory(si, config, typeof(RedisCache), typeof(RedisCacheOptions), null);
            var cache = factory.Create(null);

            // assert
            Assert.NotNull(cache);
            Assert.IsType<RedisCache>(cache);
        }

        [Fact]
        public void Create_CanReturnConnectionMultiplexer()
        {
            // arrange
            RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceName = "instanceId",
                AbortOnConnectFail = false,
                ConnectTimeout = 1
            };
            RedisServiceInfo si = new RedisServiceInfo("myId", "127.0.0.1", 4321, "sipassword")
            {
                ApplicationInfo = new ApplicationInstanceInfo()
                {
                    InstanceId = "instanceId"
                }
            };

            // act
            var factory = new RedisServiceConnectorFactory(si, config, typeof(ConnectionMultiplexer), typeof(ConfigurationOptions), RedisTypeLocator.StackExchangeInitializer);
            var multi = factory.Create(null);

            // assert
            Assert.NotNull(multi);
            Assert.IsType<ConnectionMultiplexer>(multi);
        }
    }
}
