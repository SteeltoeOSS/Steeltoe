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
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;
using System;

namespace Steeltoe.Connector.Redis
{
    public static class RedisCacheConfigurationExtensions
    {
        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.CreateRedisServiceConnectorFactory(config, serviceName);
        }

        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, IConfiguration connectorConfiguration, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (connectorConfiguration == null)
            {
                throw new ArgumentNullException(nameof(connectorConfiguration));
            }

            var connectorOptions = new RedisCacheConnectorOptions(connectorConfiguration);
            return config.CreateRedisServiceConnectorFactory(connectorOptions, serviceName);
        }

        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, RedisCacheConnectorOptions connectorOptions, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (connectorOptions == null)
            {
                throw new ArgumentNullException(nameof(connectorOptions));
            }

            var redisAssemblies = new string[] { "StackExchange.Redis", "StackExchange.Redis.StrongName", "Microsoft.Extensions.Caching.Redis" };
            var redisTypeNames = new string[] { "StackExchange.Redis.ConnectionMultiplexer", "Microsoft.Extensions.Caching.Distributed.IDistributedCache" };
            var redisOptionNames = new string[] { "StackExchange.Redis.ConfigurationOptions", "Microsoft.Extensions.Caching.Redis.RedisCacheOptions" };

            var redisConnection = ReflectionHelpers.FindType(redisAssemblies, redisTypeNames);
            var redisOptions = ReflectionHelpers.FindType(redisAssemblies, redisOptionNames);
            var initializer = ReflectionHelpers.FindMethod(redisConnection, "Connect");

            var info = serviceName == null ? config.GetSingletonServiceInfo<RedisServiceInfo>() : config.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            return new RedisServiceConnectorFactory(info, connectorOptions, redisConnection, redisOptions, initializer);
        }
    }
}
