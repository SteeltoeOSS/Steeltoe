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

namespace Steeltoe.Connector.MySql;

public static class MySqlServiceCollectionExtensions
{
    /// <summary>
    /// Add an IHealthContributor to a ServiceCollection for MySQL
    /// </summary>
    /// <param name="services">Service collection to add to</param>
    /// <param name="config">App configuration</param>
    /// <param name="contextLifetime">Lifetime of the service to inject</param>
    /// <returns>IServiceCollection for chaining</returns>
    public static IServiceCollection AddMySqlHealthContributor(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
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

        DoAdd(services, info, config, contextLifetime);
        return services;
    }

    /// <summary>
    /// Add an IHealthContributor to a ServiceCollection for MySQL
    /// </summary>
    /// <param name="services">Service collection to add to</param>
    /// <param name="config">App configuration</param>
    /// <param name="serviceName">cloud foundry service name binding</param>
    /// <param name="contextLifetime">Lifetime of the service to inject</param>
    /// <returns>IServiceCollection for chaining</returns>
    public static IServiceCollection AddMySqlHealthContributor(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
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

        DoAdd(services, info, config, contextLifetime);
        return services;
    }

    private static void DoAdd(IServiceCollection services, MySqlServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
    {
        var mySqlConfig = new MySqlProviderConnectorOptions(config);
        var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, MySqlTypeLocator.MySqlConnection);
        services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RelationalDbHealthContributor((IDbConnection)factory.Create(ctx), ctx.GetService<ILogger<RelationalDbHealthContributor>>()), contextLifetime));
    }
}
