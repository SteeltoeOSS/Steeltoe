// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Extension methods for adding services related to Spring Cloud Config Server.
/// </summary>
public static class ConfigServerServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ConfigServerClientOptions" /> for use with the options pattern.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection ConfigureConfigServerClientOptions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ConfigServerClientOptions>().Configure<IConfiguration>((options, configuration) =>
            configuration.GetSection(ConfigServerClientOptions.ConfigurationPrefix).Bind(options));

        return services;
    }

    /// <summary>
    /// Adds the <see cref="ConfigServerHealthContributor" /> as a <see cref="IHealthContributor" /> to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddConfigServerHealthContributor(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHealthContributor), typeof(ConfigServerHealthContributor)));

        return services;
    }

    /// <summary>
    /// Configures <see cref="ConfigServerClientOptions" />, hosted service and health contributor, and ensures <see cref="IConfigurationRoot" /> is
    /// available.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddConfigServerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureConfigServerClientOptions();
        services.TryAddSingleton(serviceProvider => (IConfigurationRoot)serviceProvider.GetRequiredService<IConfiguration>());
        services.AddHostedService<ConfigServerHostedService>();
        services.AddConfigServerHealthContributor();

        return services;
    }
}
