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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration.Placeholder
{
    public static class PlaceholderResolverConfigurationExtensions
    {
        /// <summary>
        /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
        /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
        /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
        /// of the applications configuration sources with place holder resolution.
        /// </summary>
        /// <param name="builder">the configuration builder</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>builder</returns>
        public static IConfigurationBuilder AddPlaceholderResolver(this IConfigurationBuilder builder, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var resolver = new PlaceholderResolverSource(builder.Sources, loggerFactory);
            builder.Sources.Clear();
            builder.Add(resolver);

            return builder;
        }

        /// <summary>
        /// Creates a new <see cref="ConfigurationRoot"/> from a <see cref="PlaceholderResolverProvider"/>.  The place holder resolver will be created using the existing
        /// configuration providers contained in the incoming configuration.  This results in providing placeholder resolution for those configuration sources.
        /// </summary>
        /// <param name="configuration">incoming configuration to wrap</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>a new configuration</returns>
        public static IConfiguration AddPlaceholderResolver(this IConfiguration configuration, ILoggerFactory loggerFactory = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var root = configuration as IConfigurationRoot;
            return new ConfigurationRoot(new List<IConfigurationProvider>() { new PlaceholderResolverProvider(new List<IConfigurationProvider>(root.Providers), loggerFactory) });
        }
    }
}
