// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System.Data;

namespace Steeltoe.Connector.SqlServer;

public static class SqlServerProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add SQL Server to a ServiceCollection.
    /// </summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="config">App configuration.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <param name="addSteeltoeHealthChecks">Add Steeltoe health check when community healthchecks exist.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
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
        DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);

        return services;
    }

    /// <summary>
    /// Add SQL Server to a ServiceCollection.
    /// </summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="config">App configuration.</param>
    /// <param name="serviceName">cloud foundry service name binding.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <param name="addSteeltoeHealthChecks">Add Steeltoe health check when community healthchecks exist.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
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
        DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);

        return services;
    }

    private static void DoAdd(IServiceCollection services, SqlServerServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime, bool addSteeltoeHealthChecks)
    {
        var sqlServerConnection = SqlServerTypeLocator.SqlConnection;
        var sqlServerConfig = new SqlServerProviderConnectorOptions(config);
        var factory = new SqlServerProviderConnectorFactory(info, sqlServerConfig, sqlServerConnection);
        services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(sqlServerConnection, factory.Create, contextLifetime));
        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalDbHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()), ServiceLifetime.Singleton));
        }
    }
}
