// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

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
    /// <paramref name="configuration" /> into the options type and adds it to the provided service container as a configured named TOption. The name of the
    /// TOption will be the <paramref name="serviceName" />. You can then inject the option using the normal Options pattern.
    /// </summary>
    /// <typeparam name="TOption">
    /// The options type.
    /// </typeparam>
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
    public static IServiceCollection ConfigureCloudFoundryService<TOption>(this IServiceCollection services, IConfiguration configuration, string serviceName)
        where TOption : CloudFoundryServicesOptions
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceName);

        services.Configure<TOption>(serviceName, option => option.Bind(configuration, serviceName));

        return services;
    }

    /// <summary>
    /// Finds all of the Cloud Foundry services with the <paramref name="serviceLabel" /> in VCAP_SERVICES and binds the configuration data from the provided
    /// <paramref name="configuration" /> into the options type and adds them all to the provided service container as a configured named TOptions. The name
    /// of each TOption will be the name of the Cloud Foundry service binding. You can then inject all the options using the normal Options pattern.
    /// </summary>
    /// <typeparam name="TOption">
    /// The options type.
    /// </typeparam>
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
    public static IServiceCollection ConfigureCloudFoundryServices<TOption>(this IServiceCollection services, IConfiguration configuration, string serviceLabel)
        where TOption : CloudFoundryServicesOptions
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNullOrEmpty(serviceLabel);

        var servicesOptions = new CloudFoundryServicesOptions(configuration);

        if (servicesOptions.Services.TryGetValue(serviceLabel, out IList<Service>? serviceList))
        {
            foreach (Service service in serviceList)
            {
                if (!string.IsNullOrEmpty(service.Name))
                {
                    services.ConfigureCloudFoundryService<TOption>(configuration, service.Name);
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
        ArgumentGuard.NotNull(services);

        ServiceDescriptor? appInfo = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IApplicationInstanceInfo));

        if (appInfo?.ImplementationType?.IsAssignableFrom(typeof(CloudFoundryApplicationOptions)) != true)
        {
            if (appInfo != null)
            {
                services.Remove(appInfo);
            }

            services.AddSingleton(typeof(CloudFoundryApplicationOptions),
                serviceProvider => new CloudFoundryApplicationOptions(serviceProvider.GetRequiredService<IConfiguration>()));

            services.AddSingleton<IApplicationInstanceInfo>(serviceProvider => serviceProvider.GetRequiredService<CloudFoundryApplicationOptions>());
        }

        return services;
    }
}
