// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.SqlServer;

public static class SqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Add an IHealthContributor to a ServiceCollection for SqlServer.
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
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddSqlServerHealthContributor(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<SqlServerServiceInfo>();

        DoAdd(services, info, configuration, contextLifetime);
        return services;
    }

    /// <summary>
    /// Add an IHealthContributor to a ServiceCollection for SqlServer.
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
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddSqlServerHealthContributor(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<SqlServerServiceInfo>(serviceName);

        DoAdd(services, info, configuration, contextLifetime);
        return services;
    }

    private static void DoAdd(IServiceCollection services, SqlServerServiceInfo info, IConfiguration configuration, ServiceLifetime contextLifetime)
    {
        var options = new SqlServerProviderConnectorOptions(configuration);
        var factory = new SqlServerProviderConnectorFactory(info, options, SqlServerTypeLocator.SqlConnection);

        services.Add(new ServiceDescriptor(typeof(IHealthContributor),
            ctx => new RelationalDbHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
            contextLifetime));
    }
}
