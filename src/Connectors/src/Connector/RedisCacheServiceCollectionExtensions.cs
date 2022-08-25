// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Redis;

public static class RedisCacheServiceCollectionExtensions
{
    /// <summary>
    /// Add IDistributedCache and its IHealthContributor to ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// RedisCache is retrievable as both RedisCache and IDistributedCache.
    /// </remarks>
    public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration configuration,
        bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        return services.AddDistributedRedisCache(configuration, configuration, null, addSteeltoeHealthChecks: addSteeltoeHealthChecks);
    }

    /// <summary>
    /// Add IDistributedCache and its IHealthContributor to ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="serviceName">
    /// Name of service to add.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// RedisCache is retrievable as both RedisCache and IDistributedCache.
    /// </remarks>
    public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration configuration, string serviceName,
        bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        return services.AddDistributedRedisCache(configuration, configuration, serviceName, addSteeltoeHealthChecks: addSteeltoeHealthChecks);
    }

    /// <summary>
    /// Add IDistributedCache and its IHealthContributor to ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="applicationConfiguration">
    /// App configuration.
    /// </param>
    /// <param name="connectorConfiguration">
    /// Connector configuration.
    /// </param>
    /// <param name="serviceName">
    /// Name of service to add.
    /// </param>
    /// <param name="contextLifetime">
    /// <see cref="ServiceLifetime" /> of the service to inject.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// RedisCache is retrievable as both RedisCache and IDistributedCache.
    /// </remarks>
    public static IServiceCollection AddDistributedRedisCache(this IServiceCollection services, IConfiguration applicationConfiguration,
        IConfiguration connectorConfiguration, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton,
        bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(applicationConfiguration);

        IConfiguration configToConfigure = connectorConfiguration ?? applicationConfiguration;

        RedisServiceInfo info = serviceName != null
            ? configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName)
            : configToConfigure.GetSingletonServiceInfo<RedisServiceInfo>();

        DoAddIDistributedCache(services, info, configToConfigure, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    /// <summary>
    /// Add Redis Connection Multiplexer and its IHealthContributor to ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// ConnectionMultiplexer is retrievable as both ConnectionMultiplexer and IConnectionMultiplexer.
    /// </remarks>
    public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration configuration,
        bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        return services.AddRedisConnectionMultiplexer(configuration, configuration, null, addSteeltoeHealthChecks: addSteeltoeHealthChecks);
    }

    /// <summary>
    /// Add Redis Connection Multiplexer and its IHealthContributor to ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="serviceName">
    /// Name of service to add.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// ConnectionMultiplexer is retrievable as both ConnectionMultiplexer and IConnectionMultiplexer.
    /// </remarks>
    public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration configuration, string serviceName,
        bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        return services.AddRedisConnectionMultiplexer(configuration, configuration, serviceName, addSteeltoeHealthChecks: addSteeltoeHealthChecks);
    }

    /// <summary>
    /// Add Redis Connection Multiplexer and its IHealthContributor to ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="applicationConfiguration">
    /// App configuration.
    /// </param>
    /// <param name="connectorConfiguration">
    /// Connector configuration.
    /// </param>
    /// <param name="serviceName">
    /// Name of service to add.
    /// </param>
    /// <param name="contextLifetime">
    /// <see cref="ServiceLifetime" /> of the service to inject.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// ConnectionMultiplexer is retrievable as both ConnectionMultiplexer and IConnectionMultiplexer.
    /// </remarks>
    public static IServiceCollection AddRedisConnectionMultiplexer(this IServiceCollection services, IConfiguration applicationConfiguration,
        IConfiguration connectorConfiguration, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton,
        bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(applicationConfiguration);

        IConfiguration configToConfigure = connectorConfiguration ?? applicationConfiguration;

        RedisServiceInfo info = serviceName == null
            ? configToConfigure.GetSingletonServiceInfo<RedisServiceInfo>()
            : configToConfigure.GetRequiredServiceInfo<RedisServiceInfo>(serviceName);

        DoAddConnectionMultiplexer(services, info, configToConfigure, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    private static void DoAddIDistributedCache(IServiceCollection services, RedisServiceInfo info, IConfiguration configuration,
        ServiceLifetime contextLifetime, bool addSteeltoeHealthChecks = false)
    {
        Type interfaceType = RedisTypeLocator.MicrosoftInterface;
        Type connectionType = RedisTypeLocator.MicrosoftImplementation;
        Type optionsType = RedisTypeLocator.MicrosoftOptions;

        var options = new RedisCacheConnectorOptions(configuration);
        var factory = new RedisServiceConnectorFactory(info, options, connectionType, optionsType, null);
        services.Add(new ServiceDescriptor(interfaceType, factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(connectionType, factory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RedisHealthContributor(factory, connectionType, ctx.GetService<ILogger<RedisHealthContributor>>()), ServiceLifetime.Singleton));
        }
    }

    private static void DoAddConnectionMultiplexer(IServiceCollection services, RedisServiceInfo info, IConfiguration configuration,
        ServiceLifetime contextLifetime, bool addSteeltoeHealthChecks)
    {
        Type redisInterface = RedisTypeLocator.StackExchangeInterface;
        Type redisImplementation = RedisTypeLocator.StackExchangeImplementation;
        Type redisOptions = RedisTypeLocator.StackExchangeOptions;
        MethodInfo initializer = RedisTypeLocator.StackExchangeInitializer;

        var options = new RedisCacheConnectorOptions(configuration);
        var factory = new RedisServiceConnectorFactory(info, options, redisImplementation, redisOptions, initializer);
        services.Add(new ServiceDescriptor(redisInterface, factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(redisImplementation, factory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RedisHealthContributor(factory, redisImplementation, ctx.GetService<ILogger<RedisHealthContributor>>()), ServiceLifetime.Singleton));
        }
    }
}
