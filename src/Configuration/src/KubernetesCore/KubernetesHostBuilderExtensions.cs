﻿// Copyright 2017 the original author or authors.
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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    public static class KubernetesHostBuilderExtensions
    {
        /// <summary>
        /// Add Kubernetes Configuration Providers for configmaps and secrets
        /// </summary>
        /// <param name="hostBuilder">Your HostBuilder</param>
        public static IWebHostBuilder AddKubernetesConfiguration(this IWebHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddKubernetesConfiguration();
                })
                .ConfigureServices(serviceCollection => serviceCollection.RegisterKubernetesApplicationInstanceInfo());

            return hostBuilder;
        }

        /// <summary>
        /// Add Kubernetes Configuration Providers for configmaps and secrets
        /// </summary>
        /// <param name="hostBuilder">Your WebHostBuilder</param>
        public static IHostBuilder AddKubernetesConfiguration(this IHostBuilder hostBuilder)
        {
            hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddKubernetesConfiguration();
                })
                .ConfigureServices((context, serviceCollection) => serviceCollection.RegisterKubernetesApplicationInstanceInfo());

            return hostBuilder;
        }
    }
}
