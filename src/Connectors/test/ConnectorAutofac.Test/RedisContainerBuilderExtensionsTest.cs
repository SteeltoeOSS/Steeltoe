// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.Common.HealthChecks;
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
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RedisContainerBuilderExtensions.RegisterDistributedRedisCache(cb, null));
        }

        [Fact]
        public void RegisterRedisCacheConnection_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
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
        public void RegisterRedisCacheConnection_AddsHealthContributorToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = RedisContainerBuilderExtensions.RegisterDistributedRedisCache(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RedisHealthContributor>(healthContributor);
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
            var cb = new ContainerBuilder();

            // act & assert
            Assert.Throws<ArgumentNullException>(() => RedisContainerBuilderExtensions.RegisterRedisConnectionMultiplexer(cb, null));
        }

        [Fact]
        public void RegisterRedisConnectionMultiplexerConnection_AddsToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            var appsettings = new Dictionary<string, string>()
            {
                ["redis:client:abortOnConnectFail"] = "false",
                ["redis:client:connectTimeout"] = "1"
            };

            var configurationBuilder = new ConfigurationBuilder();
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

        [Fact]
        public void RegisterRedisConnectionMultiplexer_AddsHealthContributorToContainer()
        {
            // arrange
            var container = new ContainerBuilder();
            IConfiguration config = new ConfigurationBuilder().Build();

            // act
            var regBuilder = RedisContainerBuilderExtensions.RegisterRedisConnectionMultiplexer(container, config);
            var services = container.Build();
            var healthContributor = services.Resolve<IHealthContributor>();

            // assert
            Assert.NotNull(healthContributor);
            Assert.IsType<RedisHealthContributor>(healthContributor);
        }
    }
}
