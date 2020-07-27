// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public static class RedisCacheConfigurationExtensions
    {
        public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, string serviceName = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var redisConfig = new RedisCacheConnectorOptions(config);
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

            var redisConnection = ConnectorHelpers.FindType(redisAssemblies, redisTypeNames);
            var redisOptions = ConnectorHelpers.FindType(redisAssemblies, redisOptionNames);
            var initializer = ConnectorHelpers.FindMethod(redisConnection, "Connect");

            var info = serviceName == null ? config.GetSingletonServiceInfo<RedisServiceInfo>() : config.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);
            return new RedisServiceConnectorFactory(info, connectorOptions, redisConnection, redisOptions, initializer ?? null);
        }
    }
}
