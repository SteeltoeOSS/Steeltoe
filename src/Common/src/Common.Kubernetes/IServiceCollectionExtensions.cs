// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Steeltoe.Common.Kubernetes
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Removes any existing <see cref="IApplicationInstanceInfo"/> if found. Registers a <see cref="KubernetesApplicationOptions" />
        /// </summary>
        /// <param name="serviceCollection">Collection of configured services</param>
        public static IServiceCollection AddKubernetesApplicationInstanceInfo(this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            var appInfo = serviceCollection.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IApplicationInstanceInfo));

            if (appInfo?.ImplementationType?.IsAssignableFrom(typeof(KubernetesApplicationOptions)) != true)
            {
                if (appInfo != null)
                {
                    serviceCollection.Remove(appInfo);
                }

                var sp = serviceCollection.BuildServiceProvider();
                var config = sp.GetRequiredService<IConfiguration>();

                var newAppInfo = new KubernetesApplicationOptions(config);
                serviceCollection.AddSingleton(newAppInfo);
                serviceCollection.AddSingleton(typeof(IApplicationInstanceInfo), newAppInfo);
            }

            return serviceCollection;
        }

        /// <summary>
        /// Retrieves <see cref="KubernetesApplicationOptions"/> from the service collection
        /// </summary>
        /// <param name="serviceCollection">Collection of configured services</param>
        /// <returns>Relevant <see cref="KubernetesApplicationOptions" /></returns>
        public static IApplicationInstanceInfo GetKubernetesApplicationOptions(this IServiceCollection serviceCollection)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddKubernetesApplicationInstanceInfo();
            var sp = serviceCollection.BuildServiceProvider();

            return sp.GetRequiredService<IApplicationInstanceInfo>();
        }

        /// <summary>
        /// Add a <see cref="IKubernetes"/> client to the service collection
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/></param>
        /// <param name="kubernetesClientConfiguration">Customization of the Kubernetes Client</param>
        /// <returns>Collection of configured services</returns>
        public static IServiceCollection AddKubernetesClient(this IServiceCollection serviceCollection, Action<KubernetesClientConfiguration> kubernetesClientConfiguration = null)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            var appInfo = serviceCollection.GetKubernetesApplicationOptions() as KubernetesApplicationOptions;

            var sp = serviceCollection.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetService<ILoggerFactory>()?.CreateLogger("Steeltoe.Common.KubernetesClientHelpers");
            serviceCollection.AddSingleton((serviceProvider) => KubernetesClientHelpers.GetKubernetesClient(appInfo, kubernetesClientConfiguration, logger));

            return serviceCollection;
        }
    }
}
