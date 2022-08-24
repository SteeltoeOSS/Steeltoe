// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Bootstrap.AutoConfiguration.TypeLocators;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MySql.EntityFramework6;

public static class MySqlDbContextServiceCollectionExtensions
{
    /// <summary>
    /// Add a MySql-backed DbContext and MySQL health contributor to the Service Collection.
    /// </summary>
    /// <typeparam name="TContext">
    /// Type of DbContext to add.
    /// </typeparam>
    /// <param name="services">
    /// Service Collection.
    /// </param>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<MySqlServiceInfo>();
        DoAdd(services, configuration, info, typeof(TContext), contextLifetime);

        return services;
    }

    /// <summary>
    /// Add a MySql-backed DbContext and MySQL health contributor to the Service Collection.
    /// </summary>
    /// <typeparam name="TContext">
    /// Type of DbContext to add.
    /// </typeparam>
    /// <param name="services">
    /// Service Collection.
    /// </param>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="serviceName">
    /// Name of service binding in Cloud Foundry.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);
        DoAdd(services, configuration, info, typeof(TContext), contextLifetime);

        return services;
    }

    private static void DoAdd(IServiceCollection services, IConfiguration configuration, MySqlServiceInfo info, Type dbContextType,
        ServiceLifetime contextLifetime)
    {
        var options = new MySqlProviderConnectorOptions(configuration);

        var factory = new MySqlDbContextConnectorFactory(info, options, dbContextType);
        services.Add(new ServiceDescriptor(dbContextType, factory.Create, contextLifetime));
        var healthFactory = new MySqlProviderConnectorFactory(info, options, MySqlTypeLocator.MySqlConnection);

        services.Add(new ServiceDescriptor(typeof(IHealthContributor),
            ctx => new RelationalDbHealthContributor((IDbConnection)healthFactory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
            contextLifetime));
    }
}
