// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Extension methods for adding services related to Spring Cloud Config Server.
/// </summary>
public static class ConfigServerServiceCollectionExtensions
{
    public static IServiceCollection ConfigureConfigServerClientOptions(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddOptions<ConfigServerClientOptions>().Configure<IConfiguration>((options, configuration) =>
            configuration.GetSection(ConfigServerClientOptions.ConfigurationPrefix).Bind(options));

        return services;
    }

    /// <summary>
    /// Adds the <see cref="ConfigServerHealthContributor" /> as a <see cref="IHealthContributor" /> to the service container.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <returns>
    /// The service collection.
    /// </returns>
    public static IServiceCollection AddConfigServerHealthContributor(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHealthContributor), typeof(ConfigServerHealthContributor)));

        return services;
    }

    /// <summary>
    /// Configures <see cref="ConfigServerClientOptions" />, hosted service and health contributor, and ensures <see cref="IConfigurationRoot" /> is
    /// available.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    public static void AddConfigServerServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureConfigServerClientOptions();
        services.TryAddSingleton(serviceProvider => (IConfigurationRoot)serviceProvider.GetRequiredService<IConfiguration>());
        services.AddHostedService<ConfigServerHostedService>();
        services.AddConfigServerHealthContributor();
    }
}
