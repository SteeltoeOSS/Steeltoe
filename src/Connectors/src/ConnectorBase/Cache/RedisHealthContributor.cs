// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            HealthCheckResult result = new HealthCheckResult();

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
            var latency = (TimeSpan)ConnectorHelpers.Invoke(_pingMethod, _database, new[] { _flags });
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
                    var getDatabaseMethod = ConnectorHelpers.FindMethod(_connector.GetType(), "GetDatabase");
                    _database = ConnectorHelpers.Invoke(getDatabaseMethod, _connector, new object[] { -1, null });
                    _pingMethod = ConnectorHelpers.FindMethod(_database.GetType(), "Ping");
                }
            }
        }
    }
}