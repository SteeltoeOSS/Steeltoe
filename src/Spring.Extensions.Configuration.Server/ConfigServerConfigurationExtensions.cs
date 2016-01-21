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
        private const string VCAP_SERVICES_PREFIX = "vcap:services";
        private const string VCAP_SERVICES_CONFIGSERVER_PREFIX = "vcap:services:p-config-server:0";

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
        /// Default Spring Config Server settings <see cref="ConfigServerClientSettings" /> will be used unless
        /// overriden by values found in providers previously added to the <paramref name="configurationBuilder"/>. 
        /// Example:
        ///         var configurationBuilder = new ConfigurationBuilder();
        ///         configurationBuilder.AddJsonFile("appsettings.json")
        ///                             .AddEnvironmentVariables()
        ///                             .AddConfigServer()
        ///                             .Build();
        ///     would use default Config Server setting unless overriden by values found in appsettings.json or
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

            settings.Name = ResovlePlaceholders(GetApplicationName(clientConfigsection, root), root);
            settings.Environment = ResovlePlaceholders(GetEnvironment(clientConfigsection, environment), root);
            settings.Label = ResovlePlaceholders(GetLabel(clientConfigsection), root);
            settings.Username = ResovlePlaceholders(GetUsername(clientConfigsection), root);
            settings.Password = ResovlePlaceholders(GetPassword(clientConfigsection), root);
            settings.Uri = ResovlePlaceholders(GetUri(clientConfigsection, root), root);
            settings.AccessTokenUri = ResovlePlaceholders(GetAccessTokenUri(clientConfigsection, root), root);
            settings.ClientId = ResovlePlaceholders(GetClientId(clientConfigsection, root), root);
            settings.ClientSecret = ResovlePlaceholders(GetClientSecret(clientConfigsection, root), root);
            settings.Enabled = GetEnabled(clientConfigsection, root);
            settings.FailFast = GetFailFast(clientConfigsection, root);
            settings.ValidateCertificates = GetCertificateValidation(clientConfigsection, root);

    }

        private static bool GetFailFast(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            var failFast = configServerSection["failFast"];
            if (!string.IsNullOrEmpty(failFast))
            {
                bool result;
                string resolved = ResovlePlaceholders(failFast, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return ConfigServerClientSettings.DEFAULT_FAILFAST;
        }

        private static bool GetEnabled(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            var enabled = configServerSection["enabled"];
            if (!string.IsNullOrEmpty(enabled))
            {
                bool result;
                string resolved = ResovlePlaceholders(enabled, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED; ;

        }

        private static string GetUri(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
        
            // First check for spring:cloud:config:uri
            var uri = configServerSection["uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            // Next check for cloudfoundry binding vcap:services:p-config-server:0:credentials:uri
            var vcapConfigServerSection = root.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX);
            uri = vcapConfigServerSection["credentials:uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            // Take default if none of above
            return ConfigServerClientSettings.DEFAULT_URI;
        }

        private static string GetPassword(IConfigurationSection configServerSection)
        {
            return configServerSection["password"];
        }

        private static string GetUsername(IConfigurationSection configServerSection)
        {
            return configServerSection["username"];
        }

        private static string GetLabel(IConfigurationSection configServerSection)
        {
            // TODO: multi label  support
            return configServerSection["label"];
        }

        private static string GetApplicationName(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            // TODO: Figure out a sensible "default" app name (e.g apps assembly name?)
            var appSection = root.GetSection(SPRING_APPLICATION_PREFIX);
            return GetSetting("name", configServerSection, appSection, null);
        }

        private static string GetEnvironment(IConfigurationSection section, IHostingEnvironment environment)
        {
            // if spring:cloud:config:env present, use it
            var env = section["env"];
            if (!string.IsNullOrEmpty(env))
            {
                return env;
            }
            
            // Otherwise use ASP.NET 5 defined value (i.e. ASPNET_ENV or Hosting:Environment) (its default is 'Production')
            return environment.EnvironmentName;
        }
        private static string GetClientSecret(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            var vcapConfigServerSection = root.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX);
            return GetSetting("credentials:client_secret", configServerSection, vcapConfigServerSection,
                ConfigServerClientSettings.DEFAULT_CLIENT_SECRET);

        }

        private static string GetClientId(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            var vcapConfigServerSection = root.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX);
            return GetSetting("credentials:client_id", configServerSection, vcapConfigServerSection,
                ConfigServerClientSettings.DEFAULT_CLIENT_ID);

        }
 
        private static string GetAccessTokenUri(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            var vcapConfigServerSection = root.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX);
            return GetSetting("credentials:access_token_uri", configServerSection, vcapConfigServerSection, 
                ConfigServerClientSettings.DEFAULT_ACCESS_TOKEN_URI);

        }
        private static bool GetCertificateValidation(IConfigurationSection configServerSection, ConfigurationRoot root)
        {
            var accept = configServerSection["validate_certificates"];
            if (!string.IsNullOrEmpty(accept))
            {
                bool result;
                string resolved = ResovlePlaceholders(accept, root);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION;

        }

        private static string ResovlePlaceholders(string property, IConfiguration config)
        {
            return PropertyPlaceholderHelper.ResovlePlaceholders(property, config);
        }

        private static string GetSetting(string key, IConfigurationSection primary, IConfigurationSection secondary, string def)
        {
            // First check for key in primary
            var setting = primary[key];
            if (!string.IsNullOrEmpty(setting))
            {
                return setting;
            }

            // Next check for key in secondary
            setting = secondary[key];
            if (!string.IsNullOrEmpty(setting))
            {
                return setting;
            }

            return def;
        }

    }
}
