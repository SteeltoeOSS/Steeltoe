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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Kubernetes;
using System;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class KubernetesHostBuilderExtensions
    {
        /// <summary>
        /// Add Kubernetes Configuration Providers for configmaps and secrets
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        /// <param name="kubernetesClientConfiguration">Customize the <see cref="KubernetesClientConfiguration"/></param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/></param>
        public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder hostBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration, ILoggerFactory loggerFactory = null)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddKubernetes(kubernetesClientConfiguration, loggerFactory);
                })
                .ConfigureServices(serviceCollection => serviceCollection.AddKubernetesApplicationInstanceInfo());

            return hostBuilder;
        }

        /// <summary>
        /// Add Kubernetes Configuration Providers for configmaps and secrets
        /// </summary>
        /// <param name="hostBuilder">Your WebHostBuilder</param>
        /// <param name="kubernetesClientConfiguration">Customize the <see cref="KubernetesClientConfiguration"/></param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/></param>
        public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder hostBuilder, Action<KubernetesClientConfiguration> kubernetesClientConfiguration, ILoggerFactory loggerFactory = null)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddKubernetes(kubernetesClientConfiguration, loggerFactory);
                })
                .ConfigureServices((context, serviceCollection) => serviceCollection.AddKubernetesApplicationInstanceInfo());

            return hostBuilder;
        }
    }
}
