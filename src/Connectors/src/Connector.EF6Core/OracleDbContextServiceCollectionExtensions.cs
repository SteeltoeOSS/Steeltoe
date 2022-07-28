// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.Oracle.EF6;
using Steeltoe.Connector.Services;
using System;
using System.Data;

namespace Steeltoe.Connector.EF6Core;

public static class OracleDbContextServiceCollectionExtensions
{
    /// <summary>
    /// Add a Oracle-backed DbContext and Oracle health contributor to the Service Collection.
    /// </summary>
    /// <typeparam name="TContext">Type of DbContext to add.</typeparam>
    /// <param name="services">Service Collection.</param>
    /// <param name="config">Application Configuration.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <param name="logFactory">logging factory.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetSingletonServiceInfo<OracleServiceInfo>();
        DoAdd(services, config, info, typeof(TContext), contextLifetime, logFactory?.CreateLogger("OracleDbContextServiceCollectionExtensions"));

        return services;
    }

    /// <summary>
    /// Add a Oracle-backed DbContext and Oracle health contributor to the Service Collection.
    /// </summary>
    /// <typeparam name="TContext">Type of DbContext to add.</typeparam>
    /// <param name="services">Service Collection.</param>
    /// <param name="config">Application Configuration.</param>
    /// <param name="serviceName">Name of service binding in Cloud Foundry.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <param name="logFactory">logging factory.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);
        DoAdd(services, config, info, typeof(TContext), contextLifetime, logFactory?.CreateLogger("OracleDbContextServiceCollectionExtensions"));

        return services;
    }

    private static void DoAdd(IServiceCollection services, IConfiguration config, OracleServiceInfo info, Type dbContextType, ServiceLifetime contextLifetime, ILogger logger = null)
    {
        var oracleConfig = new OracleProviderConnectorOptions(config);

        var factory = new OracleDbContextConnectorFactory(info, oracleConfig, dbContextType);
        services.Add(new ServiceDescriptor(dbContextType, factory.Create, contextLifetime));
        try
        {
            var healthFactory = new OracleProviderConnectorFactory(info, oracleConfig, OracleTypeLocator.OracleConnection);
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalDbHealthContributor((IDbConnection)healthFactory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()), contextLifetime));
        }
        catch (TypeLoadException)
        {
            logger?.LogWarning("Failed to add a HealthContributor for the Oracle DbContext");
        }
    }
}
