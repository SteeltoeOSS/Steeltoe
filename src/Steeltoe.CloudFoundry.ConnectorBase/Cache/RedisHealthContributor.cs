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
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Cache;
using Steeltoe.CloudFoundry.Connector.Redis;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.ConnectorBase.Cache
{
    public class RedisHealthContributor : IHealthContributor
    {
        private readonly RedisServiceConnectorFactory _factory;
        private readonly Type _implType;
        private readonly ILogger<RedisHealthContributor> _logger;

        private bool IsMicrosoftImplementation => _implType == RedisTypeLocator.MicrosoftRedisImplementation;

        public RedisHealthContributor(RedisServiceConnectorFactory factory, Type implType, ILogger<RedisHealthContributor> logger)
        {
            _factory = factory;
            _implType = implType;
            _logger = logger;
        }

        public string Id => IsMicrosoftImplementation ? "redis-cache" : "redis";

        public Health Health()
        {
            _logger.LogInformation($"Checking redis connection health!");
            Health result = new Health();
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

                result.Details.Add("status", HealthStatus.UP.ToString());
                result.Status = HealthStatus.UP;
                _logger.LogInformation($"Redis connection up!");
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                {
                    e = e.InnerException;
                }
                _logger.LogInformation($"Redis connection down!");
                result.Details.Add("error", e.GetType().Name + ": " + e.Message);
                result.Details.Add("status", HealthStatus.DOWN.ToString());
                result.Status = HealthStatus.DOWN;
            }

            return result;
        }

        private void DoStackExchangeHealth(Health health)
        {
            var commandFlagsEnum = RedisTypeLocator.StackExchangeRedisCommandFlagsNames;
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