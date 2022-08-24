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
using Steeltoe.Connector.Oracle;
using Steeltoe.Connector.Oracle.EntityFramework6;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.EntityFramework6;

public static class OracleDbContextServiceCollectionExtensions
{
    /// <summary>
    /// Add a Oracle-backed DbContext and Oracle health contributor to the Service Collection.
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
    /// <param name="logFactory">
    /// logging factory.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<OracleServiceInfo>();
        DoAdd(services, configuration, info, typeof(TContext), contextLifetime, logFactory?.CreateLogger("OracleDbContextServiceCollectionExtensions"));

        return services;
    }

    /// <summary>
    /// Add a Oracle-backed DbContext and Oracle health contributor to the Service Collection.
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
    /// <param name="logFactory">
    /// logging factory.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, ILoggerFactory logFactory = null)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<OracleServiceInfo>(serviceName);
        DoAdd(services, configuration, info, typeof(TContext), contextLifetime, logFactory?.CreateLogger("OracleDbContextServiceCollectionExtensions"));

        return services;
    }

    private static void DoAdd(IServiceCollection services, IConfiguration configuration, OracleServiceInfo info, Type dbContextType,
        ServiceLifetime contextLifetime, ILogger logger = null)
    {
        var options = new OracleProviderConnectorOptions(configuration);

        var factory = new OracleDbContextConnectorFactory(info, options, dbContextType);
        services.Add(new ServiceDescriptor(dbContextType, factory.Create, contextLifetime));

        try
        {
            var healthFactory = new OracleProviderConnectorFactory(info, options, OracleTypeLocator.OracleConnection);

            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RelationalDbHealthContributor((IDbConnection)healthFactory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
                contextLifetime));
        }
        catch (TypeLoadException exception)
        {
            logger?.LogWarning(exception, "Failed to add a HealthContributor for the Oracle DbContext");
        }
    }
}
