//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Spring.Extensions.Configuration.Server
{
    /// <summary>
    /// Extension methods for adding <see cref="ConfigServerConfigurationProvider"/>.
    /// </summary>
    public static class ConfigServerConfigurationExtensions
    {
        private const string SPRING_APPLICATION_PREFIX = "spring:application";

        /// <summary>
        /// The prefix (<see cref="IConfigurationSection"/> under which all Spring Cloud Config Server 
        /// configuration settings (<see cref="ConfigServerClientSettings"/> are found. 
        ///   (e.g. spring:cloud:config:env, spring:cloud:config:uri, spring:cloud:config:enabled, etc.)
        /// </summary>
        public const string PREFIX = "spring:cloud:config";

        /// <summary>
        /// Adds the Spring Cloud Config Server provider <see cref="ConfigServerConfigurationProvider"/> 
        /// to <paramref name="configurationBuilder"/>.
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="environment">The hosting enviroment settings.</param>
        /// <param name="logFactory">optional logging factory. Used to enable logging in Config Server client.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        /// 
        /// Default Spring Config Server settings <see cref="ConfigServerClientSettings" /> will used by the provider
        /// unless overriden by values found in providers previously added to the <paramref name="configurationBuilder"/>. 
        /// Example:
        ///         var configurationBuilder = new ConfigurationBuilder();
        ///         configurationBuilder.AddJsonFile("appsettings.json")
        ///                             .AddEnvironmentVariables()
        ///                             .AddConfigServer()
        ///                             .Build();
        ///     would used default Config Server setting unless overriden by values found in appsettings.json or
        ///     environment variables.
        /// </summary>
        public static IConfigurationBuilder AddConfigServer(this IConfigurationBuilder configurationBuilder, IHostingEnvironment environment, ILoggerFactory logFactory = null)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }

            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            ConfigServerClientSettings settings = CreateSettings(environment, configurationBuilder.Providers, logFactory);
            configurationBuilder.Add(new ConfigServerConfigurationProvider(settings, logFactory));

            return configurationBuilder;

        }



        private static ConfigServerClientSettings CreateSettings(IHostingEnvironment environment, IEnumerable<IConfigurationProvider> providers, ILoggerFactory logFactory)
        {
            ConfigServerClientSettings settings = new ConfigServerClientSettings();

            if (providers != null)
            {
                ConfigurationRoot existing = new ConfigurationRoot(new List<IConfigurationProvider>(providers));
                Initialize(settings, environment, existing);
            }
   
            return settings;

        }

        private static void Initialize(ConfigServerClientSettings settings, IHostingEnvironment environment, ConfigurationRoot root)
        {

            var clientConfigsection = root.GetSection(PREFIX);
            var appSection = root.GetSection(SPRING_APPLICATION_PREFIX);

            settings.Name = GetApplicationName(clientConfigsection, appSection);
            settings.Environment = GetEnvironment(clientConfigsection, environment);
            settings.Label = GetLabel(clientConfigsection);
            settings.Username = GetUsername(clientConfigsection);
            settings.Password = GetPassword(clientConfigsection);
            settings.Uri = GetUri(clientConfigsection);
            settings.Enabled = GetEnabled(clientConfigsection);
            settings.FailFast = GetFailFast(clientConfigsection);
        }

        private static bool GetFailFast(IConfigurationSection section)
        {
            var failFast = section["failFast"];
            if (!string.IsNullOrEmpty(failFast))
            {
                bool result;
                if (Boolean.TryParse(failFast, out result))
                    return result;
            }
            return ConfigServerClientSettings.DEFAULT_FAILFAST;
        }

        private static bool GetEnabled(IConfigurationSection section)
        {
            var enabled = section["enabled"];
            if (!string.IsNullOrEmpty(enabled))
            {
                bool result;
                if (Boolean.TryParse(enabled, out result))
                    return result;
            }
            return ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED; ;

        }

        private static string GetUri(IConfigurationSection section)
        {
            var uri = section["uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                return uri;
            }
            return ConfigServerClientSettings.DEFAULT_URI;
        }

        private static string GetPassword(IConfigurationSection section)
        {
            return section["password"];
        }

        private static string GetUsername(IConfigurationSection section)
        {
            return section["username"];
        }

        private static string GetLabel(IConfigurationSection section)
        {
            // TODO: multi label  support
            return section["label"];
        }

        private static string GetApplicationName(IConfigurationSection section, IConfigurationSection appSection)
        {
            // if spring:cloud:config:name present, use it
            var name = section["name"];
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            // if spring:application:name present, use it
            name = appSection["name"];
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            // TODO: Figure out a sensible "default" app name (e.g apps assembly name?)
            return "name";
        }

        private static string GetEnvironment(IConfigurationSection section, IHostingEnvironment environment)
        {
            // if spring:cloud:config:environment present, use it
            var env = section["env"];
            if (!string.IsNullOrEmpty(env))
            {
                return env;
            }
            
            // Otherwise use frameworks defined value (its default is Production)
            return environment.EnvironmentName;
        }
    }
}
