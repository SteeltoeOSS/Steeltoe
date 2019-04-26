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
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// Extension methods for adding services related to Spring Cloud Config Server.
    /// </summary>
    public static class ConfigServerServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureConfigServerClientOptions(this IServiceCollection services, IConfiguration config)
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

            var section = config.GetSection(ConfigServerClientSettingsOptions.CONFIGURATION_PREFIX);
            services.Configure<ConfigServerClientSettingsOptions>(section);

            return services;
        }

        public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration config)
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

            services.TryAddSingleton<IConfiguration>(config);

            var root = config as IConfigurationRoot;
            if (root != null)
            {
                services.TryAddSingleton<IConfigurationRoot>(root);
            }

            return services;
        }
    }
}
