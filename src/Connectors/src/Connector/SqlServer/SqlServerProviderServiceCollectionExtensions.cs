// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.SqlServer;

public static class SqlServerProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add SQL Server to a ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<SqlServerServiceInfo>();
        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);

        return services;
    }

    /// <summary>
    /// Add SQL Server to a ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="serviceName">
    /// cloud foundry service name binding.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe health check when community healthchecks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddSqlServerConnection(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);
        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);

        return services;
    }

    private static void DoAdd(IServiceCollection services, SqlServerServiceInfo info, IConfiguration configuration, ServiceLifetime contextLifetime,
        bool addSteeltoeHealthChecks)
    {
        Type sqlServerConnection = SqlServerTypeLocator.SqlConnection;
        var options = new SqlServerProviderConnectorOptions(configuration);
        var factory = new SqlServerProviderConnectorFactory(info, options, sqlServerConnection);
        services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(sqlServerConnection, factory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RelationalDbHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
                ServiceLifetime.Singleton));
        }
    }
}
