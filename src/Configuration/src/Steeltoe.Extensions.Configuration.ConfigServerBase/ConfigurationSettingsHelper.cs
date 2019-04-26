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

            settings.Name = GetApplicationName(clientConfigsection, config, settings.Name);
            settings.Environment = GetEnvironment(clientConfigsection, config, settings.Environment);
            settings.Label = GetLabel(clientConfigsection, config);
            settings.Username = GetUsername(clientConfigsection, config);
            settings.Password = GetPassword(clientConfigsection, config);
            settings.Uri = GetUri(clientConfigsection, config, settings.Uri);
            settings.Enabled = GetEnabled(clientConfigsection, config, settings.Enabled);
            settings.FailFast = GetFailFast(clientConfigsection, config, settings.FailFast);
            settings.ValidateCertificates = GetCertificateValidation(clientConfigsection, config, settings.ValidateCertificates);
            settings.RetryEnabled = GetRetryEnabled(clientConfigsection, config, settings.RetryEnabled);
            settings.RetryInitialInterval = GetRetryInitialInterval(clientConfigsection, config, settings.RetryInitialInterval);
            settings.RetryMaxInterval = GetRetryMaxInterval(clientConfigsection, config, settings.RetryMaxInterval);
            settings.RetryMultiplier = GetRetryMultiplier(clientConfigsection, config, settings.RetryMultiplier);
            settings.RetryAttempts = GetRetryMaxAttempts(clientConfigsection, config, settings.RetryAttempts);
            settings.Token = GetToken(clientConfigsection, config);
            settings.Timeout = GetTimeout(clientConfigsection, config, settings.Timeout);
        }

        private static int GetRetryMaxAttempts(IConfigurationSection clientConfigsection, IConfiguration resolve, int def)
        {
            return ConfigurationValuesHelper.GetInt("retry:maxAttempts", clientConfigsection, resolve, def);
        }

        private static double GetRetryMultiplier(IConfigurationSection clientConfigsection, IConfiguration resolve, double def)
        {
            return ConfigurationValuesHelper.GetDouble("retry:multiplier", clientConfigsection, resolve, def);
        }

        private static int GetRetryMaxInterval(IConfigurationSection clientConfigsection, IConfiguration resolve, int def)
        {
            return ConfigurationValuesHelper.GetInt("retry:maxInterval", clientConfigsection, resolve, def);
        }

        private static int GetRetryInitialInterval(IConfigurationSection clientConfigsection, IConfiguration resolve, int def)
        {
            return ConfigurationValuesHelper.GetInt("retry:initialInterval", clientConfigsection, resolve, def);
        }

        private static bool GetRetryEnabled(IConfigurationSection clientConfigsection, IConfiguration resolve, bool def)
        {
            return ConfigurationValuesHelper.GetBoolean("retry:enabled", clientConfigsection, resolve, def);
        }

        private static bool GetFailFast(IConfigurationSection clientConfigsection, IConfiguration resolve, bool def)
        {
            return ConfigurationValuesHelper.GetBoolean("failFast", clientConfigsection, resolve, def);
        }

        private static bool GetEnabled(IConfigurationSection clientConfigsection, IConfiguration resolve, bool def)
        {
            return ConfigurationValuesHelper.GetBoolean("enabled", clientConfigsection, resolve, def);
        }

        private static string GetToken(IConfigurationSection clientConfigsection, IConfiguration resolve)
        {
            return ConfigurationValuesHelper.GetString("token", clientConfigsection, resolve, null);
        }

        private static int GetTimeout(IConfigurationSection clientConfigsection, IConfiguration resolve, int def)
        {
            return ConfigurationValuesHelper.GetInt("timeout", clientConfigsection, resolve, def);
        }

        private static string GetUri(IConfigurationSection clientConfigsection, IConfiguration resolve, string def)
        {
            return ConfigurationValuesHelper.GetString("uri", clientConfigsection, resolve, def);
        }

        private static string GetPassword(IConfigurationSection clientConfigsection, IConfiguration resolve)
        {
            return ConfigurationValuesHelper.GetString("password", clientConfigsection, resolve, null);
        }

        private static string GetUsername(IConfigurationSection clientConfigsection, IConfiguration resolve)
        {
            return ConfigurationValuesHelper.GetString("username", clientConfigsection, resolve, null);
        }

        private static string GetLabel(IConfigurationSection clientConfigsection, IConfiguration resolve)
        {
            return ConfigurationValuesHelper.GetString("label", clientConfigsection, resolve, null);
        }

        private static string GetApplicationName(IConfigurationSection primary, IConfiguration config, string defName)
        {
            var secondary = config.GetSection(SPRING_APPLICATION_PREFIX);
            return ConfigurationValuesHelper.GetSetting("name", primary, secondary, config, defName);
        }

        private static string GetEnvironment(IConfigurationSection section, IConfiguration resolve, string def)
        {
            if (string.IsNullOrEmpty(def))
            {
                def = "Production";
            }

            return ConfigurationValuesHelper.GetString("env", section, resolve, def);
        }

        private static bool GetCertificateValidation(IConfigurationSection clientConfigsection, IConfiguration resolve, bool def)
        {
            return ConfigurationValuesHelper.GetBoolean("validate_certificates", clientConfigsection, resolve, def);
        }
    }
}
