// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MySql;

public static class MySqlProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add MySql and its IHealthContributor to a ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="config">
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
    /// MySqlConnection is retrievable as both MySqlConnection and IDbConnection.
    /// </remarks>
    public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration config,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetSingletonServiceInfo<MySqlServiceInfo>();

        DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    /// <summary>
    /// Add MySql and its IHealthContributor to a ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="config">
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
    /// MySqlConnection is retrievable as both MySqlConnection and IDbConnection.
    /// </remarks>
    public static IServiceCollection AddMySqlConnection(this IServiceCollection services, IConfiguration config, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
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

        var info = config.GetRequiredServiceInfo<MySqlServiceInfo>(serviceName);

        DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    private static void DoAdd(IServiceCollection services, MySqlServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime,
        bool addSteeltoeHealthChecks)
    {
        Type mySqlConnection = ReflectionHelpers.FindType(MySqlTypeLocator.Assemblies, MySqlTypeLocator.ConnectionTypeNames);
        var mySqlConfig = new MySqlProviderConnectorOptions(config);
        var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, mySqlConnection);
        services.Add(new ServiceDescriptor(typeof(IDbConnection), factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(mySqlConnection, factory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new RelationalDbHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()),
                ServiceLifetime.Singleton));
        }
    }
}
