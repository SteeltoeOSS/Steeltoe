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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    /// <summary>
    /// Extension methods for adding <see cref="ConfigServerConfigurationProvider"/>.
    /// </summary>
    public static class ConfigServerConfigurationBuilderExtensionsCore
    {
        [Obsolete("IHostingEnvironment is obsolete")]
        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostingEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
        }

        [Obsolete("IHostingEnvironment is obsolete")]
        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
        }

        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return DoAddConfigServer(configurationBuilder, environment.ApplicationName, environment.EnvironmentName, logFactory);
        }

        private static IConfigurationBuilder DoAddConfigServer(IConfigurationBuilder configurationBuilder, string applicationName, string environmentName, ILoggerFactory logFactory)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            var settings = new ConfigServerClientSettings()
            {
                Name = applicationName,
                Environment = environmentName
            };

            return configurationBuilder.AddConfigServer(settings, logFactory);
        }
    }
}
