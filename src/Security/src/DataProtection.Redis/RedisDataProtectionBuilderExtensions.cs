// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Steeltoe.Connectors;
using Steeltoe.Connectors.Redis;

namespace Steeltoe.Security.DataProtection.Redis;

public static class RedisDataProtectionBuilderExtensions
{
    private const string DataProtectionKeysName = "DataProtection-Keys";

    private static readonly Action<RedisCacheOptions> EmptyAction = _ =>
    {
    };

    /// <summary>
    /// Configures the data protection system to persist keys in a Redis database, using the Steeltoe Connector for Redis.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IDataProtectionBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder)
    {
        return PersistKeysToRedis(builder, string.Empty);
    }

    /// <summary>
    /// Configures the data protection system to persist keys in a Redis database, using the Steeltoe Connector for Redis.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IDataProtectionBuilder" /> to configure.
    /// </param>
    /// <param name="serviceBindingName">
    /// The service binding name, in case multiple redis services are used.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IDataProtectionBuilder PersistKeysToRedis(this IDataProtectionBuilder builder, string serviceBindingName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(serviceBindingName);

        builder.Services.AddStackExchangeRedisCache(EmptyAction);

        builder.Services.AddOptions<RedisCacheOptions>().Configure<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>((options, connectorFactory) =>
            ConfigureRedisCacheOptions(options, serviceBindingName, connectorFactory));

        builder.Services.AddOptions<KeyManagementOptions>().Configure<ConnectorFactory<RedisOptions, IConnectionMultiplexer>>((options, connectorFactory) =>
            ConfigureKeyManagementOptions(options, serviceBindingName, connectorFactory));

        return builder;
    }

    private static void ConfigureRedisCacheOptions(RedisCacheOptions options, string serviceBindingName,
        ConnectorFactory<RedisOptions, IConnectionMultiplexer> connectorFactory)
    {
        IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer(connectorFactory, serviceBindingName);

        options.ConnectionMultiplexerFactory = () => Task.FromResult(connectionMultiplexer);
        options.InstanceName = connectionMultiplexer.ClientName;
    }

    private static void ConfigureKeyManagementOptions(KeyManagementOptions options, string serviceBindingName,
        ConnectorFactory<RedisOptions, IConnectionMultiplexer> connectorFactory)
    {
        options.XmlRepository = new RedisXmlRepository(() =>
        {
            IConnectionMultiplexer connectionMultiplexer = GetConnectionMultiplexer(connectorFactory, serviceBindingName);
            return connectionMultiplexer.GetDatabase();
        }, DataProtectionKeysName);
    }

    private static IConnectionMultiplexer GetConnectionMultiplexer(ConnectorFactory<RedisOptions, IConnectionMultiplexer> connectorFactory,
        string serviceBindingName)
    {
        Connector<RedisOptions, IConnectionMultiplexer> connector = connectorFactory.Get(serviceBindingName);
        return connector.GetConnection();
    }
}
