// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.PostgreSql;

public static class PostgreSqlProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add NpgsqlConnection and its IHealthContributor to a ServiceCollection.
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
    /// Add Steeltoe healthChecks.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// NpgsqlConnection is retrievable as both NpgsqlConnection and DbConnection.
    /// </remarks>
    public static IServiceCollection AddPostgreSqlConnection(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<PostgreSqlServiceInfo>();

        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    /// <summary>
    /// Add NpgsqlConnection and its IHealthContributor to a ServiceCollection.
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
    /// Add Steeltoe healthChecks.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// NpgsqlConnection is retrievable as both NpgsqlConnection and DbConnection.
    /// </remarks>
    public static IServiceCollection AddPostgreSqlConnection(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<PostgreSqlServiceInfo>(serviceName);

        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    private static void DoAdd(IServiceCollection services, PostgreSqlServiceInfo info, IConfiguration configuration, ServiceLifetime contextLifetime,
        bool addSteeltoeHealthChecks)
    {
        Type postgreSqlConnection = ReflectionHelpers.FindType(PostgreSqlTypeLocator.Assemblies, PostgreSqlTypeLocator.ConnectionTypeNames);
        var options = new PostgreSqlProviderConnectorOptions(configuration);
        var factory = new PostgreSqlProviderConnectorFactory(info, options, postgreSqlConnection);
        services.Add(new ServiceDescriptor(typeof(DbConnection), factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(postgreSqlConnection, factory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RelationalDbHealthContributor((DbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
                ServiceLifetime.Singleton));
        }
    }
}
