//
// Copyright 2017 the original author or authors.
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

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Configuration;
using System;


namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public static class ConfigurationSettingsHelper
    {
        private const string SPRING_APPLICATION_PREFIX = "spring:application";

        public static void Initialize(string configPrefix, ConfigServerClientSettings settings, IConfiguration config)
        {
            if (configPrefix == null)
            {
                throw new ArgumentNullException(nameof(configPrefix));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }


            var clientConfigsection = config.GetSection(configPrefix);

            settings.Name = ResolvePlaceholders(GetApplicationName(clientConfigsection, config, settings.Name), config);
            settings.Environment = ResolvePlaceholders(GetEnvironment(clientConfigsection, settings.Environment), config);
            settings.Label = ResolvePlaceholders(GetLabel(clientConfigsection), config);
            settings.Username = ResolvePlaceholders(GetUsername(clientConfigsection), config);
            settings.Password = ResolvePlaceholders(GetPassword(clientConfigsection), config);
            settings.Uri = ResolvePlaceholders(GetUri(clientConfigsection, config, settings.Uri), config);
            settings.Enabled = GetEnabled(clientConfigsection, config, settings.Enabled);
            settings.FailFast = GetFailFast(clientConfigsection, config, settings.FailFast);
            settings.ValidateCertificates = GetCertificateValidation(clientConfigsection, config, settings.ValidateCertificates);
            settings.RetryEnabled = GetRetryEnabled(clientConfigsection, config, settings.RetryEnabled);
            settings.RetryInitialInterval = GetRetryInitialInterval(clientConfigsection, config, settings.RetryInitialInterval);
            settings.RetryMaxInterval = GetRetryMaxInterval(clientConfigsection, config, settings.RetryMaxInterval);
            settings.RetryMultiplier = GetRetryMultiplier(clientConfigsection, config, settings.RetryMultiplier);
            settings.RetryAttempts = GetRetryMaxAttempts(clientConfigsection, config, settings.RetryAttempts);
            settings.Token = GetToken(clientConfigsection);
            settings.Timeout = GetTimeout(clientConfigsection, settings.Timeout);
        }

        private static int GetRetryMaxAttempts(IConfigurationSection clientConfigsection, IConfiguration config, int def)
        {
            return GetInt("retry:maxAttempts", clientConfigsection, config, def);
        }

        private static double GetRetryMultiplier(IConfigurationSection clientConfigsection, IConfiguration config, double def)
        {
            return GetDouble("retry:multiplier", clientConfigsection, config, def);
        }

        private static int GetRetryMaxInterval(IConfigurationSection clientConfigsection, IConfiguration config, int def)
        {
            return GetInt("retry:maxInterval", clientConfigsection, config, def);
        }

        private static int GetRetryInitialInterval(IConfigurationSection clientConfigsection, IConfiguration config, int def)
        {
            return GetInt("retry:initialInterval", clientConfigsection, config, def);
        }

        private static bool GetRetryEnabled(IConfigurationSection clientConfigsection, IConfiguration config, bool def)
        {
            return GetBoolean("retry:enabled", clientConfigsection, config, def);
        }

        private static bool GetFailFast(IConfigurationSection clientConfigsection, IConfiguration config, bool def)
        {
            return GetBoolean("failFast", clientConfigsection, config, def);
        }

        private static bool GetEnabled(IConfigurationSection clientConfigsection, IConfiguration config, bool def)
        {
            return GetBoolean("enabled", clientConfigsection, config, def);
        }

        private static string GetToken(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["token"];
        }

        private static int GetTimeout(IConfigurationSection clientConfigsection, int def)
        {
            var val = clientConfigsection["timeout"];
            if (!string.IsNullOrEmpty(val))
            {
                int result;
                if (int.TryParse(val, out result))
                    return result;
            }
            return def;
        }

        private static string GetUri(IConfigurationSection clientConfigsection, IConfiguration config, string def)
        {

            // First check for spring:cloud:config:uri
            var uri = clientConfigsection["uri"];
            if (!string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            // Take default if none of above
            return def;
        }

        private static string GetPassword(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["password"];
        }

        private static string GetUsername(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["username"];
        }

        private static string GetLabel(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection["label"];
        }


        private static string GetApplicationName(IConfigurationSection clientConfigsection, IConfiguration config, string defName)
        {
            var appSection = config.GetSection(SPRING_APPLICATION_PREFIX);
            return GetSetting("name", clientConfigsection, appSection, defName);
        }

        private static string GetEnvironment(IConfigurationSection section, string environment)
        {
            // if spring:cloud:config:env present, use it
            var env = section["env"];
            if (!string.IsNullOrEmpty(env))
            {
                return env;
            }

            if (string.IsNullOrEmpty(environment))
            {
                return "Production";
            }

            return environment;
        }

        private static bool GetCertificateValidation(IConfigurationSection clientConfigsection, IConfiguration config, bool def)
        {
            return GetBoolean("validate_certificates", clientConfigsection, config, def);
        }

        private static string ResolvePlaceholders(string property, IConfiguration config)
        {
            return PropertyPlaceholderHelper.ResolvePlaceholders(property, config);
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

        private static int GetInt(string key, IConfigurationSection clientConfigsection, IConfiguration config, int def)
        {
            var val = clientConfigsection[key];
            if (!string.IsNullOrEmpty(val))
            {
                int result;
                string resolved = ResolvePlaceholders(val, config);
                if (int.TryParse(resolved, out result))
                    return result;
            }
            return def;
        }
        private static double GetDouble(string key, IConfigurationSection clientConfigsection, IConfiguration config, double def)
        {
            var val = clientConfigsection[key];
            if (!string.IsNullOrEmpty(val))
            {
                double result;
                string resolved = ResolvePlaceholders(val, config);
                if (double.TryParse(resolved, out result))
                    return result;
            }
            return def;
        }

        private static bool GetBoolean(string key, IConfigurationSection clientConfigsection, IConfiguration config, bool def)
        {
            var val = clientConfigsection[key];
            if (!string.IsNullOrEmpty(val))
            {
                bool result;
                string resolved = ResolvePlaceholders(val, config);
                if (Boolean.TryParse(resolved, out result))
                    return result;
            }
            return def;

        }

    }
}
