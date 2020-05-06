// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            if (appInfo != null)
            {
                serviceCollection.Remove(appInfo);
            }

            var sp = serviceCollection.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();

            var newAppInfo = new KubernetesApplicationOptions(config);
            serviceCollection.AddSingleton(typeof(IApplicationInstanceInfo), newAppInfo);

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
            serviceCollection.AddSingleton((serviceProvider) => KubernetesClientHelpers.GetKubernetesClient(config, appInfo, kubernetesClientConfiguration, logger));

            return serviceCollection;
        }
    }
}
