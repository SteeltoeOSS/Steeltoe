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

using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Cache
{
    public class RedisHealthContributorTest
    {
        [Fact]
        public void StackExchange_Not_Connected_Returns_Down_Status()
        {
            // arrange
            var redisOptions = new RedisCacheConnectorOptions() { ConnectTimeout = 1 };
            var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6378");
            var logrFactory = new LoggerFactory();
            var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.StackExchangeImplementation, RedisTypeLocator.StackExchangeOptions, RedisTypeLocator.StackExchangeInitializer);
            var h = new RedisHealthContributor(connFactory, RedisTypeLocator.StackExchangeImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Equal("Redis health check failed", status.Description);
        }

        [Fact/*(Skip = "Integration test - Requires local server")*/]
        public void StackExchange_Is_Connected_Returns_Up_Status()
        {
            // arrange
            var redisOptions = new RedisCacheConnectorOptions();
            var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6379");
            var logrFactory = new LoggerFactory();
            var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.StackExchangeImplementation, RedisTypeLocator.StackExchangeOptions, RedisTypeLocator.StackExchangeInitializer);
            var h = new RedisHealthContributor(connFactory, RedisTypeLocator.StackExchangeImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }

        [Fact]
        public void Microsoft_Not_Connected_Returns_Down_Status()
        {
            // arrange
            var redisOptions = new RedisCacheConnectorOptions() { ConnectTimeout = 1 };
            var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6378");
            var logrFactory = new LoggerFactory();
            var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.MicrosoftImplementation, RedisTypeLocator.MicrosoftOptions, null);
            var h = new RedisHealthContributor(connFactory, RedisTypeLocator.MicrosoftImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Equal("Redis health check failed", status.Description);
        }

        [Fact/*(Skip = "Integration test - Requires local server")*/]
        public void Microsoft_Is_Connected_Returns_Up_Status()
        {
            // arrange
            var redisOptions = new RedisCacheConnectorOptions();
            var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6379");
            var logrFactory = new LoggerFactory();
            var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.MicrosoftImplementation, RedisTypeLocator.MicrosoftOptions, null);
            var h = new RedisHealthContributor(connFactory, RedisTypeLocator.MicrosoftImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }
    }
}
