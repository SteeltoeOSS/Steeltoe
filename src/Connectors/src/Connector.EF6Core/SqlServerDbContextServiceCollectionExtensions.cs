// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System;
using System.Data;

namespace Steeltoe.Connector.SqlServer.EF6;

public static class SqlServerDbContextServiceCollectionExtensions
{
    /// <summary>
    /// Add a Microsoft SQL Server-backed DbContext and SQL Server health contributor to the Service Collection.
    /// </summary>
    /// <typeparam name="TContext">Type of DbContext to add.</typeparam>
    /// <param name="services">Service Collection.</param>
    /// <param name="config">Application Configuration.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetSingletonServiceInfo<SqlServerServiceInfo>();
        DoAdd(services, config, info, typeof(TContext), contextLifetime);

        return services;
    }

    /// <summary>
    /// Add a Microsoft SQL Server-backed DbContext to the Service Collection.
    /// </summary>
    /// <typeparam name="TContext">Type of DbContext to add.</typeparam>
    /// <param name="services">Service Collection.</param>
    /// <param name="config">Application Configuration.</param>
    /// <param name="serviceName">Name of service binding in Cloud Foundry.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
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

        var info = config.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
        DoAdd(services, config, info, typeof(TContext), contextLifetime);

        return services;
    }

    private static void DoAdd(IServiceCollection services, IConfiguration config, SqlServerServiceInfo info, Type dbContextType, ServiceLifetime contextLifetime)
    {
        var sqlServerConfig = new SqlServerProviderConnectorOptions(config);

        var factory = new SqlServerDbContextConnectorFactory(info, sqlServerConfig, dbContextType);
        services.Add(new ServiceDescriptor(dbContextType, factory.Create, contextLifetime));
        var healthFactory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, SqlServerTypeLocator.SqlConnection);
        services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalDbHealthContributor((IDbConnection)healthFactory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()), contextLifetime));
    }
}
