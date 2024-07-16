// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.Redis.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis;

public static class RedisServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RedisOptions" /> and
    /// StackExchange.Redis.IConnectionMultiplexer) to connect to a Redis database. If Microsoft.Extensions.Caching.StackExchangeRedis is referenced, this
    /// method additionally registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> with type parameters <see cref="RedisOptions" /> and
    /// <see cref="IDistributedCache" />.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        return AddRedis(services, configuration, StackExchangeRedisPackageResolver.Default, MicrosoftRedisPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RedisOptions" /> and
    /// StackExchange.Redis.IConnectionMultiplexer) to connect to a Redis database. If Microsoft.Extensions.Caching.StackExchangeRedis is referenced, this
    /// method additionally registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> with type parameters <see cref="RedisOptions" /> and
    /// <see cref="IDistributedCache" />.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="addAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddRedis(services, configuration, StackExchangeRedisPackageResolver.Default, MicrosoftRedisPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration,
        StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver, MicrosoftRedisPackageResolver microsoftRedisPackageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(stackExchangeRedisPackageResolver);
        ArgumentGuard.NotNull(microsoftRedisPackageResolver);

        if (!ConnectorFactoryShim<RedisOptions>.IsRegistered(stackExchangeRedisPackageResolver.ConnectionMultiplexerInterface.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateConnectionMultiplexer(serviceProvider, serviceBindingName, stackExchangeRedisPackageResolver),
                CreateHealthContributor)
            {
                // From https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/Basics.md:
                //   "Because the ConnectionMultiplexer does a lot, it is designed to be shared and reused between callers.
                //   You should not create a ConnectionMultiplexer per operation."
                CacheConnection = true,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<RedisOptions>(services, configuration, "redis",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<RedisOptions>.Register(stackExchangeRedisPackageResolver.ConnectionMultiplexerInterface.Type, services, optionNames,
                optionsBuilder.CreateConnection, optionsBuilder.CacheConnection);

            if (microsoftRedisPackageResolver.IsAvailable())
            {
                ConnectorCreateConnection createDistributedCache = (serviceProvider, serviceBindingName) => CreateDistributedCache(
                    optionsBuilder.CreateConnection, serviceProvider, serviceBindingName, stackExchangeRedisPackageResolver, microsoftRedisPackageResolver);

                ConnectorFactoryShim<RedisOptions>.Register(microsoftRedisPackageResolver.DistributedCacheInterface.Type, services, optionNames,
                    createDistributedCache, optionsBuilder.CacheConnection);
            }
        }

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName)
    {
        // Not using the Steeltoe ConnectorFactory here, because obtaining a connection throws when Redis is down at application startup.

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
        string? connectionString = optionsMonitor.Get(serviceBindingName).ConnectionString;

        var logger = serviceProvider.GetRequiredService<ILogger<RedisHealthContributor>>();

        return new RedisHealthContributor(connectionString, logger)
        {
            ServiceName = serviceBindingName
        };
    }

    private static IDisposable CreateConnectionMultiplexer(IServiceProvider serviceProvider, string serviceBindingName,
        StackExchangeRedisPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RedisOptions>>();
        RedisOptions options = optionsMonitor.Get(serviceBindingName);

        ConnectionMultiplexerInterfaceShim connectionMultiplexerInterfaceShim = ConnectionMultiplexerShim.Connect(packageResolver, options.ConnectionString);
        return connectionMultiplexerInterfaceShim.Instance;
    }

    private static object CreateDistributedCache(ConnectorCreateConnection stackExchangeCreateConnection, IServiceProvider serviceProvider,
        string serviceBindingName, StackExchangeRedisPackageResolver stackExchangeRedisPackageResolver,
        MicrosoftRedisPackageResolver microsoftRedisPackageResolver)
    {
        object connectionMultiplexerInstance = stackExchangeCreateConnection(serviceProvider, serviceBindingName);
        var connectionMultiplexerShim = new ConnectionMultiplexerInterfaceShim(stackExchangeRedisPackageResolver, connectionMultiplexerInstance);

        var redisCacheOptionsShim = RedisCacheOptionsShim.CreateInstance(microsoftRedisPackageResolver);
        redisCacheOptionsShim.InstanceName = connectionMultiplexerShim.ClientName;
        redisCacheOptionsShim.ConnectionMultiplexerFactory = redisCacheOptionsShim.CreateTaskLambdaForConnectionMultiplexerFactory(connectionMultiplexerShim);

        var redisCacheShim = RedisCacheShim.CreateInstance(microsoftRedisPackageResolver, redisCacheOptionsShim);
        return redisCacheShim.Instance;
    }
}
