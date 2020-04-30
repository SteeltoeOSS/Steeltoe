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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Kubernetes;
using System;

namespace Steeltoe.Extensions.Configuration.Kubernetes
{
    /// <summary>
    /// Extension methods for adding services related to Kubernetes
    /// </summary>
    public static class KubernetesServiceCollectionExtensions
    {
        /// <summary>
        /// Bind configuration data into <see cref="KubernetesApplicationOptions"/>
        /// and add to the provided service container as configured TOptions.
        /// </summary>
        /// <param name="services">the service container</param>
        /// <param name="config">the applications configuration</param>
        /// <returns>service container</returns>
        public static IServiceCollection ConfigureKubernetesOptions(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.AddOptions();

            var appSection = config.GetSection(KubernetesApplicationOptions.PlatformConfigRoot);
            services.Configure<KubernetesApplicationOptions>(appSection);

            return services;
        }
    }
}
