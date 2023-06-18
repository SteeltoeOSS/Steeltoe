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

namespace Steeltoe.Connectors.MySql;

public static class MySqlProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add MySql and its IHealthContributor to a ServiceCollection.
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
    /// Add steeltoeHealth checks even if community health checks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// MySqlConnection is retrievable as both MySqlConnection and DbConnection.
    /// </remarks>
    public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<MySqlServiceInfo>();

        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    /// <summary>
    /// Add MySql and its IHealthContributor to a ServiceCollection.
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
    /// Add steeltoeHealth checks even if community health checks exist.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    /// <remarks>
    /// MySqlConnection is retrievable as both MySqlConnection and DbConnection.
    /// </remarks>
    public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    private static void DoAdd(IServiceCollection services, MySqlServiceInfo info, IConfiguration configuration, ServiceLifetime contextLifetime,
        bool addSteeltoeHealthChecks)
    {
        Type mySqlConnection = ReflectionHelpers.FindType(MySqlTypeLocator.Assemblies, MySqlTypeLocator.ConnectionTypeNames);
        var options = new MySqlProviderConnectorOptions(configuration);
        var factory = new MySqlProviderConnectorFactory(info, options, mySqlConnection);
        services.Add(new ServiceDescriptor(typeof(DbConnection), factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(mySqlConnection, factory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RelationalDbHealthContributor((DbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
                ServiceLifetime.Singleton));
        }
    }
}
