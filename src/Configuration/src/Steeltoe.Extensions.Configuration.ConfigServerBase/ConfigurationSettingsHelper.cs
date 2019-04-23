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
        private const string VCAP_SERVICES_CONFIGSERVER_PREFIX = "vcap:services:p-config-server:0";

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
            settings.Environment = GetEnvironment(clientConfigsection, settings.Environment);
            settings.Label = GetLabel(clientConfigsection);
            settings.Username = GetUsername(clientConfigsection);
            settings.Password = GetPassword(clientConfigsection);
            settings.Uri = GetUri(clientConfigsection, settings.Uri);
            settings.Enabled = GetEnabled(clientConfigsection, settings.Enabled);
            settings.FailFast = GetFailFast(clientConfigsection, settings.FailFast);
            settings.ValidateCertificates = GetCertificateValidation(clientConfigsection, settings.ValidateCertificates);
            settings.RetryEnabled = GetRetryEnabled(clientConfigsection, settings.RetryEnabled);
            settings.RetryInitialInterval = GetRetryInitialInterval(clientConfigsection, settings.RetryInitialInterval);
            settings.RetryMaxInterval = GetRetryMaxInterval(clientConfigsection, settings.RetryMaxInterval);
            settings.RetryMultiplier = GetRetryMultiplier(clientConfigsection, settings.RetryMultiplier);
            settings.RetryAttempts = GetRetryMaxAttempts(clientConfigsection, settings.RetryAttempts);
            settings.Token = GetToken(clientConfigsection);
            settings.Timeout = GetTimeout(clientConfigsection, settings.Timeout);
            settings.AccessTokenUri = GetAccessTokenUri(clientConfigsection, config);
            settings.ClientId = GetClientId(clientConfigsection, config);
            settings.ClientSecret = GetClientSecret(clientConfigsection, config);
            settings.TokenRenewRate = GetTokenRenewRate(clientConfigsection);
            settings.TokenTtl = GetTokenTtl(clientConfigsection);
            settings.DiscoveryEnabled = GetDiscoveryEnabled(clientConfigsection, settings.DiscoveryEnabled);
            settings.DiscoveryServiceId = GetDiscoveryServiceId(clientConfigsection, settings.DiscoveryServiceId);
            settings.HealthEnabled = GetHealthEnabled(clientConfigsection, settings.HealthEnabled);
            settings.HealthTimeToLive = GetHealthTimeToLive(clientConfigsection, settings.HealthTimeToLive);

            // Override Config server URI
            settings.Uri = GetCloudFoundryUri(clientConfigsection, config, settings.Uri);
        }

        private static bool GetHealthEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("health:enabled", def);
        }

        private static long GetHealthTimeToLive(IConfigurationSection clientConfigsection, long def)
        {
            return clientConfigsection.GetValue("health:timeToLive", def);
        }

        private static bool GetDiscoveryEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("discovery:enabled", def);
        }

        private static string GetDiscoveryServiceId(IConfigurationSection clientConfigsection, string def)
        {
            return clientConfigsection.GetValue("discovery:serviceId", def);
        }

        private static int GetRetryMaxAttempts(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("retry:maxAttempts", def);
        }

        private static double GetRetryMultiplier(IConfigurationSection clientConfigsection, double def)
        {
            return clientConfigsection.GetValue("retry:multiplier", def);
        }

        private static int GetRetryMaxInterval(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("retry:maxInterval", def);
        }

        private static int GetRetryInitialInterval(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("retry:initialInterval", def);
        }

        private static bool GetRetryEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("retry:enabled", def);
        }

        private static bool GetFailFast(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("failFast", def);
        }

        private static bool GetEnabled(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("enabled", def);
        }

        private static string GetToken(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("token");
        }

        private static int GetTimeout(IConfigurationSection clientConfigsection, int def)
        {
            return clientConfigsection.GetValue("timeout", def);
        }

        private static string GetUri(IConfigurationSection clientConfigsection, string def)
        {
            return clientConfigsection.GetValue("uri", def);
        }

        private static string GetPassword(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("password");
        }

        private static string GetUsername(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("username");
        }

        private static string GetLabel(IConfigurationSection clientConfigsection)
        {
            return clientConfigsection.GetValue<string>("label");
        }

        private static string GetEnvironment(IConfigurationSection section, string def)
        {
            return section.GetValue("env", string.IsNullOrEmpty(def) ? ConfigServerClientSettings.DEFAULT_ENVIRONMENT : def);
        }

        private static bool GetCertificateValidation(IConfigurationSection clientConfigsection, bool def)
        {
            return clientConfigsection.GetValue("validateCertificates", def) && clientConfigsection.GetValue("validate_certificates", def);
        }

        private static int GetTokenRenewRate(IConfigurationSection configServerSection)
        {
            return configServerSection.GetValue("tokenRenewRate", ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_RENEW_RATE);
        }

        private static int GetTokenTtl(IConfigurationSection configServerSection)
        {
            return configServerSection.GetValue("tokenTtl", ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_TTL);
        }

        private static string GetClientSecret(IConfigurationSection configServerSection, IConfiguration config)
        {
            return GetSetting("credentials:client_secret", config.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX), configServerSection, ConfigServerClientSettings.DEFAULT_CLIENT_SECRET);
        }

        private static string GetClientId(IConfigurationSection configServerSection, IConfiguration config)
        {
            return GetSetting("credentials:client_id", config.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX), configServerSection, ConfigServerClientSettings.DEFAULT_CLIENT_ID);
        }

        private static string GetAccessTokenUri(IConfigurationSection configServerSection, IConfiguration config)
        {
            return GetSetting("credentials:access_token_uri", config.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX), configServerSection, ConfigServerClientSettings.DEFAULT_ACCESS_TOKEN_URI);
        }

        private static string GetApplicationName(IConfigurationSection primary, IConfiguration config, string defName)
        {
            return GetSetting("name", primary, config.GetSection(SPRING_APPLICATION_PREFIX), defName);
        }

        private static string GetCloudFoundryUri(IConfiguration configServerSection, IConfiguration config, string def)
        {
            return GetSetting("credentials:uri", config.GetSection(VCAP_SERVICES_CONFIGSERVER_PREFIX), configServerSection, def);
        }

        private static string GetSetting(string key, IConfiguration primary, IConfiguration secondary, string def)
        {
            var result = primary.GetValue<string>(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            result = secondary.GetValue<string>(key);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return def;
        }
    }
}
