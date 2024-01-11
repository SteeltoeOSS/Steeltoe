// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Kubernetes;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Removes any existing <see cref="IApplicationInstanceInfo" /> if found. Registers a <see cref="KubernetesApplicationOptions" />.
    /// </summary>
    /// <param name="services">
    /// Collection of configured services.
    /// </param>
    public static IServiceCollection AddKubernetesApplicationInstanceInfo(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        ServiceDescriptor appInfoDescriptor = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IApplicationInstanceInfo));

        if (appInfoDescriptor?.ImplementationType?.IsAssignableFrom(typeof(KubernetesApplicationOptions)) != true)
        {
            if (appInfoDescriptor != null)
            {
                services.Remove(appInfoDescriptor);
            }

            services.AddSingleton(typeof(KubernetesApplicationOptions),
                serviceProvider => new KubernetesApplicationOptions(serviceProvider.GetRequiredService<IConfiguration>()));

            services.AddSingleton(typeof(IApplicationInstanceInfo), serviceProvider => serviceProvider.GetRequiredService<KubernetesApplicationOptions>());
        }

        return services;
    }

    /// <summary>
    /// Add a <see cref="IKubernetes" /> client to the service collection.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    /// <param name="configureKubernetesClient">
    /// Enables to configure the Kubernetes client.
    /// </param>
    /// <returns>
    /// Collection of configured services.
    /// </returns>
    public static IServiceCollection AddKubernetesClient(this IServiceCollection services,
        Action<KubernetesClientConfiguration> configureKubernetesClient = null)
    {
        ArgumentGuard.NotNull(services);

        services.AddKubernetesApplicationInstanceInfo();

        services.TryAddSingleton(serviceProvider =>
        {
            ILogger logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger(typeof(KubernetesClientHelpers).FullName!);
            var appInfo = serviceProvider.GetRequiredService<KubernetesApplicationOptions>();
            return KubernetesClientHelpers.GetKubernetesClient(appInfo, configureKubernetesClient, logger);
        });

        return services;
    }
}
