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
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Reflection;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public static class ConfigServerConfigurationBuilderExtensions
    {
        private const string DEFAULT_ENVIRONMENT = "Production";

        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ILoggerFactory logFactory = null)
        {
            return configurationBuilder.AddConfigServer(DEFAULT_ENVIRONMENT, Assembly.GetEntryAssembly()?.GetName().Name, logFactory);
        }

        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, ILoggerFactory logFactory = null)
        {
            return configurationBuilder.AddConfigServer(environment, Assembly.GetEntryAssembly()?.GetName().Name, logFactory);
        }

        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, string environment, string applicationName, ILoggerFactory logFactory = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            var settings = new ConfigServerClientSettings()
            {
                Name = applicationName ?? Assembly.GetEntryAssembly()?.GetName().Name,
                Environment = environment ?? DEFAULT_ENVIRONMENT
            };

            return configurationBuilder.AddConfigServer(settings, logFactory);
        }

        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, ConfigServerClientSettings defaultSettings, ILoggerFactory logFactory = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (defaultSettings == null)
            {
                throw new ArgumentNullException(nameof(defaultSettings));
            }

            configurationBuilder.Add(new ConfigServerConfigurationProvider(defaultSettings, logFactory));
            return configurationBuilder;
        }
    }
}
