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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.Connector.Redis
{
    public class RedisHealthContributor : IHealthContributor
    {
        public static IHealthContributor GetRedisContributor(IConfiguration configuration, ILogger<RedisHealthContributor> logger = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var redisImplementation = RedisTypeLocator.StackExchangeImplementation;
            var redisOptions = RedisTypeLocator.StackExchangeOptions;
            var initializer = RedisTypeLocator.StackExchangeInitializer;

            var info = configuration.GetSingletonServiceInfo<RedisServiceInfo>();
            var redisConfig = new RedisCacheConnectorOptions(configuration);
            var factory = new RedisServiceConnectorFactory(info, redisConfig, redisImplementation, redisOptions, initializer);
            return new RedisHealthContributor(factory, redisImplementation, logger);
        }

        private readonly RedisServiceConnectorFactory _factory;
        private readonly Type _implType;
        private readonly ILogger<RedisHealthContributor> _logger;
        private object _connector;
        private object _database;
        private object _flags;
        private MethodInfo _pingMethod;
        private MethodInfo _getMethod;

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
            var result = new HealthCheckResult();

            try
            {
                Connect();

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
            var latency = (TimeSpan)ReflectionHelpers.Invoke(_pingMethod, _database, new[] { _flags });
            health.Details.Add("ping", latency.TotalMilliseconds);
        }

        private void DoMicrosoftHealth()
        {
            _getMethod.Invoke(_connector, new object[] { "1" });
        }

        private void Connect()
        {
            if (_connector == null)
            {
                _connector = _factory.Create(null);
                if (_connector == null)
                {
                    throw new ConnectorException("Unable to connect to Redis");
                }

                if (IsMicrosoftImplementation)
                {
                    _getMethod = _connector.GetType().GetMethod("Get");
                }
                else
                {
                    var commandFlagsEnum = RedisTypeLocator.StackExchangeCommandFlagsNames;
                    _flags = Enum.Parse(commandFlagsEnum, "None");
                    var getDatabaseMethod = ReflectionHelpers.FindMethod(_connector.GetType(), "GetDatabase");
                    _database = ReflectionHelpers.Invoke(getDatabaseMethod, _connector, new object[] { -1, null });
                    _pingMethod = ReflectionHelpers.FindMethod(_database.GetType(), "Ping");
                }
            }
        }
    }
}