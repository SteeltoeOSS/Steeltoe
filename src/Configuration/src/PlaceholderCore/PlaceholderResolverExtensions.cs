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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.Placeholder;
using System;

namespace Steeltoe.Extensions.Configuration.PlaceholderCore
{
    public static class PlaceholderResolverExtensions
    {
        /// <summary>
        /// Create a new <see cref="IConfiguration"/> using a <see cref="PlaceholderResolverProvider"/> which wraps the provided <see cref="IConfiguration"/>.
        /// The new configuration will then be used to replace the current <see cref="IConfiguration"/> in the service container. All subsequent requests for a
        /// <see cref="IConfiguration"/> will return the newly created one providing placeholder resolution.
        /// </summary>
        /// <param name="services">the current service container</param>
        /// <param name="configuration">the configuration the placeholder resolver will wrap</param>
        /// <param name="loggerFactory">the log factory to use</param>
        /// <returns>the new configuration</returns>
        public static IConfiguration ConfigurePlaceholderResolver(this IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var newConfig = configuration.AddPlaceholderResolver(loggerFactory);

            services.Replace(ServiceDescriptor.Singleton(typeof(IConfiguration), newConfig));

            return newConfig;
        }

        /// <summary>
        /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
        /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
        /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
        /// of the applications configuration sources with place holder resolution.
        /// </summary>
        /// <param name="hostBuilder">the host builder</param>
        /// <param name="loggerFactory">the log factory to use</param>
        /// <returns>provided host builder</returns>
        public static IWebHostBuilder AddPlaceholderResolver(this IWebHostBuilder hostBuilder, ILoggerFactory loggerFactory = null)
        {
            hostBuilder.ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddPlaceholderResolver(loggerFactory);
            });
            return hostBuilder;
        }

        /// <summary>
        /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
        /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
        /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
        /// of the applications configuration sources with place holder resolution.
        /// </summary>
        /// <param name="hostBuilder">the host builder</param>
        /// <param name="loggerFactory">the log factory to use</param>
        /// <returns>provided host builder</returns>
        public static IHostBuilder AddPlaceholderResolver(this IHostBuilder hostBuilder, ILoggerFactory loggerFactory = null)
        {
            hostBuilder.ConfigureAppConfiguration((context, builder) =>
            {
                builder.AddPlaceholderResolver(loggerFactory);
            });
            return hostBuilder;
        }
    }
}
