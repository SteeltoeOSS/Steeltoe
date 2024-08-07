// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry;

/// <summary>
/// Extension methods for adding services related to CloudFoundry.
/// </summary>
public static class CloudFoundryServiceCollectionExtensions
{
    /// <summary>
    /// Binds configuration data into <see cref="CloudFoundryApplicationOptions" /> and <see cref="CloudFoundryServicesOptions" /> and adds both to the
    /// provided service container. You can then inject both options using the normal Options pattern.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The application configuration.
    /// </param>
    /// <returns>
    /// The incoming service container.
    /// </returns>
    public static IServiceCollection ConfigureCloudFoundryOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions();

        IConfigurationSection appSection =
            configuration.GetSection($"{CloudFoundryApplicationOptions.PlatformConfigurationRoot}:{ApplicationInstanceInfo.ApplicationRoot}");

        services.Configure<CloudFoundryApplicationOptions>(appSection);

        IConfigurationSection serviceSection = configuration.GetSection(CloudFoundryServicesOptions.ServicesConfigurationRoot);
        services.Configure<CloudFoundryServicesOptions>(serviceSection);

        return services;
    }

    /// <summary>
    /// Finds the Cloud Foundry service with the <paramref name="serviceName" /> in VCAP_SERVICES and binds the configuration data from the provided
    /// <paramref name="configuration" /> into <see cref="CloudFoundryServicesOptions" />. The name of each option will be the name of the Cloud Foundry
    /// service binding. You can then inject all the options using the normal options pattern.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The application configuration.
    /// </param>
    /// <param name="serviceName">
    /// The Cloud Foundry service name to bind to the options type.
    /// </param>
    /// <returns>
    /// The incoming service container.
    /// </returns>
    public static IServiceCollection ConfigureCloudFoundryService(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.Configure<CloudFoundryServicesOptions>(serviceName, option => option.Bind(configuration, serviceName));

        return services;
    }

    /// <summary>
    /// Finds all Cloud Foundry services with the <paramref name="serviceLabel" /> in VCAP_SERVICES and binds the configuration data from the provided
    /// <paramref name="configuration" /> into <see cref="CloudFoundryServicesOptions" />. The name of each option will be the name of the Cloud Foundry
    /// service binding. You can then inject all the options using the normal options pattern.
    /// </summary>
    /// <param name="services">
    /// The service container.
    /// </param>
    /// <param name="configuration">
    /// The application configuration.
    /// </param>
    /// <param name="serviceLabel">
    /// The Cloud Foundry service label to use to bind to the options type.
    /// </param>
    /// <returns>
    /// The incoming service container.
    /// </returns>
    public static IServiceCollection ConfigureCloudFoundryServices(this IServiceCollection services, IConfiguration configuration, string serviceLabel)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceLabel);

        var servicesOptions = new CloudFoundryServicesOptions(configuration);

        if (servicesOptions.Services.TryGetValue(serviceLabel, out IList<Service>? serviceList))
        {
            foreach (Service service in serviceList)
            {
                if (!string.IsNullOrEmpty(service.Name))
                {
                    services.ConfigureCloudFoundryService(configuration, service.Name);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Removes any existing <see cref="IApplicationInstanceInfo" /> if found. Registers a <see cref="CloudFoundryApplicationOptions" />.
    /// </summary>
    /// <param name="services">
    /// Collection of configured services.
    /// </param>
    public static IServiceCollection RegisterCloudFoundryApplicationInstanceInfo(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton<IApplicationInstanceInfo>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            return new CloudFoundryApplicationOptions(configuration);
        }));

        return services;
    }
}
