// Copyright 2017 the original author or authors.
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

using Autofac;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.ConnectorAutofac.Test
{
    public class RedisContainerBuilderExtensionsTest
    {
        [Fact]
        public void RegisterRedisCacheConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RedisContainerBuilderExtensions.RegisterDistributedRedisCache(null, config));
        }

        [Fact]
        public void RegisterRedisCacheConnection_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RedisContainerBuilderExtensions.RegisterDistributedRedisCache(cb, null));
        }

        [Fact]
        public void RegisterRedisCacheConnection_AddsToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = RedisContainerBuilderExtensions.RegisterDistributedRedisCache(container, config);
            var services = container.Build();
            var redisCache = services.Resolve<RedisCache>();
            var redisCacheFromI = services.Resolve<IDistributedCache>();

            // assert
            Assert.NotNull(redisCache);
            Assert.IsType<RedisCache>(redisCache);
            Assert.NotNull(redisCacheFromI);
            Assert.IsType<RedisCache>(redisCacheFromI);
        }

        [Fact]
        public void RegisterRedisConnectionMultiplexerConnection_Requires_Builder()
        {
            // arrange
            IConfiguration config = new ConfigurationBuilder().Build();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RedisContainerBuilderExtensions.RegisterRedisConnectionMultiplexer(null, config));
        }

        [Fact]
        public void RegisterRedisConnectionMultiplexerConnection_Requires_Config()
        {
            // arrange
            ContainerBuilder cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RedisContainerBuilderExtensions.RegisterRedisConnectionMultiplexer(cb, null));
        }

        [Fact]
        public void RegisterRedisConnectionMultiplexerConnection_AddsToContainer()
        {
            // arrange
            ContainerBuilder container = new ContainerBuilder();
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:abortOnConnectFail"] = "false",
                ["redis:client:connectTimeout"] = "1"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            // act
            var regBuilder = RedisContainerBuilderExtensions.RegisterRedisConnectionMultiplexer(container, config);
            var services = container.Build();
            var redisConnectionMultiplexer = services.Resolve<ConnectionMultiplexer>();
            var redisIConnectionMultiplexer = services.Resolve<IConnectionMultiplexer>();

            // assert
            Assert.NotNull(redisConnectionMultiplexer);
            Assert.IsType<ConnectionMultiplexer>(redisConnectionMultiplexer);
            Assert.NotNull(redisIConnectionMultiplexer);
            Assert.IsType<ConnectionMultiplexer>(redisIConnectionMultiplexer);
        }
    }
}
