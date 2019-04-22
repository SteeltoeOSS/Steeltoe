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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public class RedisHealthContributor : IHealthContributor
    {
        public static IHealthContributor GetRedisContributor(IConfiguration configuration, ILogger<RedisHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Type redisImplementation = RedisTypeLocator.StackExchangeImplementation;
            Type redisOptions = RedisTypeLocator.StackExchangeOptions;
            MethodInfo initializer = RedisTypeLocator.StackExchangeInitializer;

            var info = configuration.GetSingletonServiceInfo<RedisServiceInfo>();
            RedisCacheConnectorOptions redisConfig = new RedisCacheConnectorOptions(configuration);
            RedisServiceConnectorFactory factory = new RedisServiceConnectorFactory(info, redisConfig, redisImplementation, redisOptions, initializer);
            return new RedisHealthContributor(factory, redisImplementation, logger);
        }

        private readonly RedisServiceConnectorFactory _factory;
        private readonly Type _implType;
        private readonly ILogger<RedisHealthContributor> _logger;

        private bool IsMicrosoftImplementation => _implType.FullName.Contains("Microsoft");

        public RedisHealthContributor(RedisServiceConnectorFactory factory, Type implType, ILogger<RedisHealthContributor> logger = null)
        {
            _factory = factory;
            _implType = implType;
            _logger = logger;
        }

        public string Id => IsMicrosoftImplementation ? "Redis-Cache" : "Redis";

        public HealthCheckResult Health()
        {
            _logger?.LogTrace("Checking Redis connection health");
            HealthCheckResult result = new HealthCheckResult();
            try
            {
                if (IsMicrosoftImplementation)
                {
                    DoMicrosoftHealth();
                }
                else
                {
                    DoStackExchangeHealth(result);
                }

                // Spring Boot health checks also include cluster size and slot metrics
                result.Details.Add("status", HealthStatus.UP.ToString());
                result.Status = HealthStatus.UP;
                _logger?.LogTrace("Redis connection up!");
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                {
                    e = e.InnerException;
                }

                _logger?.LogError("Redis connection down! {HealthCheckException}", e.Message);
                result.Details.Add("error", e.GetType().Name + ": " + e.Message);
                result.Details.Add("status", HealthStatus.DOWN.ToString());
                result.Status = HealthStatus.DOWN;
                result.Description = "Redis health check failed";
            }

            return result;
        }

        private void DoStackExchangeHealth(HealthCheckResult health)
        {
            var commandFlagsEnum = RedisTypeLocator.StackExchangeCommandFlagsNames;
            var none = Enum.Parse(commandFlagsEnum, "None");
            var connector = _factory.Create(null);

            var getDatabaseMethod = ConnectorHelpers.FindMethod(connector.GetType(), "GetDatabase");
            var db = ConnectorHelpers.Invoke(getDatabaseMethod, connector, new object[] { -1, null });
            var pingMethod = ConnectorHelpers.FindMethod(db.GetType(), "Ping");
            var latency = (TimeSpan)ConnectorHelpers.Invoke(pingMethod, db, new[] { none });
            health.Details.Add("ping", latency.TotalMilliseconds);
        }

        private void DoMicrosoftHealth()
        {
            var connector = _factory.Create(null);
            var getMethod = connector.GetType().GetMethod("Get");
            getMethod.Invoke(connector, new object[] { "1" });
        }
    }
}