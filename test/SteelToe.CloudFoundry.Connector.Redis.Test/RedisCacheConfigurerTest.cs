//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.Extensions.Caching.Redis;
using SteelToe.CloudFoundry.Connector.App;
using SteelToe.CloudFoundry.Connector.Services;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Redis.Test
{
    public class RedisCacheConfigurerTest
    {
        [Fact]
        public void UpdateOptions_FromConfig_WithConnectionString_ReturnsExcpected()
        {
            RedisCacheConfigurer configurer = new RedisCacheConfigurer();
            RedisCacheOptions redisOptions = new RedisCacheOptions();
            RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
            {
                ConnectionString = "foobar",
                InstanceId = "instanceId"
            };
            configurer.UpdateOptions(config, redisOptions);

            Assert.Equal("foobar", redisOptions.Configuration);
            Assert.Equal("instanceId", redisOptions.InstanceName);

        }

        [Fact]
        public void UpdateOptions_FromConfig_WithOutConnectionString_ReturnsExcpected()
        {
            RedisCacheConfigurer configurer = new RedisCacheConfigurer();
            RedisCacheOptions redisOptions = new RedisCacheOptions();
            RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password ="password",
                InstanceId = "instanceId"
            };
            configurer.UpdateOptions(config, redisOptions);

            Assert.Equal( "localhost:1234,password=password", redisOptions.Configuration);
            Assert.Equal("instanceId", redisOptions.InstanceName);

        }

        [Fact]
        public void UpdateOptions_FromServiceInfo_ReturnsExcpected()
        {
            RedisCacheConfigurer configurer = new RedisCacheConfigurer();
            RedisCacheOptions redisOptions = new RedisCacheOptions();
            RedisServiceInfo si = new RedisServiceInfo("myId", "foobar", 4321, "sipassword");
            si.ApplicationInfo = new ApplicationInstanceInfo()
            {
                InstanceId = "instanceId"
            };
            configurer.UpdateOptions(si, redisOptions);

            Assert.Equal("foobar:4321,password=sipassword", redisOptions.Configuration);
            Assert.Equal("instanceId", redisOptions.InstanceName);

        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            RedisCacheConfigurer configurer = new RedisCacheConfigurer();
            RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceId = "instanceId"
            };
            var opts = configurer.Configure(null, config);
            Assert.NotNull(opts);
            var redisOptions = opts.Value;
            Assert.NotNull(redisOptions);

            Assert.Equal("localhost:1234,password=password", redisOptions.Configuration);
            Assert.Equal("instanceId", redisOptions.InstanceName);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            RedisCacheConfigurer configurer = new RedisCacheConfigurer();
            RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceId = "instanceId"
            };
            RedisServiceInfo si = new RedisServiceInfo("myId", "foobar", 4321, "sipassword");
            si.ApplicationInfo = new ApplicationInstanceInfo()
            {
                InstanceId = "instanceId"
            };
            var opts = configurer.Configure(si, config);
            Assert.NotNull(opts);
            var redisOptions = opts.Value;
            Assert.NotNull(redisOptions);

            Assert.Equal("foobar:4321,password=sipassword", redisOptions.Configuration);
            Assert.Equal("instanceId", redisOptions.InstanceName);
        }
    }
}
