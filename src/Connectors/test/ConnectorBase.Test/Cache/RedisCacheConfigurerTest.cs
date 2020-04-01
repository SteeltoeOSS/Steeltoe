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
using Steeltoe.Connector.Services;
using System.Net;
using Xunit;

namespace Steeltoe.Connector.Redis.Test
{
    [Collection("Redis")]
    public class RedisCacheConfigurerTest
    {
        // [Fact]
        // public void UpdateOptions_FromConfig_WithConnectionString_ReturnsExpected()
        // {
        //    RedisCacheConfigurer configurer = new RedisCacheConfigurer();
        //    RedisCacheOptions redisOptions = new RedisCacheOptions();
        //    RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
        //    {
        //        ConnectionString = "foobar",
        //        InstanceName = "instanceId"
        //    };
        //    configurer.UpdateOptions(config, redisOptions);
        //    Assert.Equal("foobar", redisOptions.Configuration);
        //    Assert.Equal("instanceId", redisOptions.InstanceName);
        // }

        // [Fact]
        // public void UpdateOptions_FromConfig_WithOutConnectionString_ReturnsExcpected()
        // {
        //    RedisCacheConfigurer configurer = new RedisCacheConfigurer();
        //    RedisCacheOptions redisOptions = new RedisCacheOptions();
        //    RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
        //    {
        //        Host = "localhost",
        //        Port = 1234,
        //        Password = "password",
        //        InstanceName = "instanceId"
        //    };
        //    configurer.UpdateOptions(config, redisOptions);
        //    Assert.Equal("localhost:1234,password=password,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", redisOptions.Configuration);
        //    Assert.Equal("instanceId", redisOptions.InstanceName);
        //// }

        [Fact]
        public void UpdateOptions_FromServiceInfo_ReturnsExcpected()
        {
            var configurer = new RedisCacheConfigurer();
            var connOptions = new RedisCacheConnectorOptions();
            var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");
            configurer.UpdateOptions(si, connOptions);

            Assert.Equal("foobar:4321,password=sipassword,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connOptions.ToString());
            Assert.Null(connOptions.InstanceName);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            var configurer = new RedisCacheConfigurer();
            var config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceName = "instanceId"
            };
            var opts = configurer.Configure(null, config);
            Assert.NotNull(opts);
            var redisOptions = (RedisCacheOptions)opts.ToMicrosoftExtensionObject(typeof(RedisCacheOptions));
            Assert.NotNull(redisOptions);

            Assert.Equal("localhost:1234,password=password,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", redisOptions.Configuration);
            Assert.Equal("instanceId", redisOptions.InstanceName);
        }

        [Fact]
        public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            var configurer = new RedisCacheConfigurer();
            var config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password",
                InstanceName = "instanceId"
            };
            var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");
            var connectionSettings = configurer.Configure(si, config);
            Assert.NotNull(connectionSettings);

            Assert.Equal("foobar:4321,password=sipassword,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionSettings.ToString());
            Assert.Equal("instanceId", connectionSettings.InstanceName);
        }

        [Fact]
        public void ConfigureConnection_NoServiceInfo_ReturnsExpected()
        {
            // arrange
            var configurer = new RedisCacheConfigurer();
            var config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password"
            };

            // act
            var opts = configurer.Configure(null, config);
            Assert.NotNull(opts);

            // assert
            Assert.NotNull(((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints);
            var ep = ((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints[0] as DnsEndPoint;
            Assert.NotNull(ep);
            Assert.Equal("localhost", ep.Host);
            Assert.Equal(1234, ep.Port);
            Assert.Equal("password", opts.Password);
        }

        [Fact]
        public void ConfigureConnection_ServiceInfoOveridesConfig_ReturnsExpected()
        {
            // arrange
            var configurer = new RedisCacheConfigurer();
            var config = new RedisCacheConnectorOptions()
            {
                Host = "localhost",
                Port = 1234,
                Password = "password"
            };
            var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");

            // act
            var opts = configurer.Configure(si, config);
            Assert.NotNull(opts);

            // assert
            Assert.NotNull(((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints);
            var ep = ((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints[0] as DnsEndPoint;
            Assert.NotNull(ep);
            Assert.Equal("foobar", ep.Host);
            Assert.Equal(4321, ep.Port);
            Assert.Equal("sipassword", opts.Password);
        }
    }
}