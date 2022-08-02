// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Redis;

public static class RedisCacheConfigurationExtensions
{
    public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, string serviceName = null)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var redisConfig = new RedisCacheConnectorOptions(config);
        return config.CreateRedisServiceConnectorFactory(redisConfig, serviceName);
    }

    public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, IConfiguration connectorConfiguration,
        string serviceName = null)
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

    public static RedisServiceConnectorFactory CreateRedisServiceConnectorFactory(this IConfiguration config, RedisCacheConnectorOptions connectorOptions,
        string serviceName = null)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (connectorOptions == null)
        {
            throw new ArgumentNullException(nameof(connectorOptions));
        }

        string[] redisAssemblies = new[]
        {
            "StackExchange.Redis",
            "StackExchange.Redis.StrongName",
            "Microsoft.Extensions.Caching.Redis"
        };

        string[] redisTypeNames = new[]
        {
            "StackExchange.Redis.ConnectionMultiplexer",
            "Microsoft.Extensions.Caching.Distributed.IDistributedCache"
        };

        string[] redisOptionNames = new[]
        {
            "StackExchange.Redis.ConfigurationOptions",
            "Microsoft.Extensions.Caching.Redis.RedisCacheOptions"
        };

        Type redisConnection = ReflectionHelpers.FindType(redisAssemblies, redisTypeNames);
        Type redisOptions = ReflectionHelpers.FindType(redisAssemblies, redisOptionNames);
        MethodInfo initializer = ReflectionHelpers.FindMethod(redisConnection, "Connect");

        RedisServiceInfo info = serviceName == null
            ? config.GetSingletonServiceInfo<RedisServiceInfo>()
            : config.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);

        return new RedisServiceConnectorFactory(info, connectorOptions, redisConnection, redisOptions, initializer);
    }
}
