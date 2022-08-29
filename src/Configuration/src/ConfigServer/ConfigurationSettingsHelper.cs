// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

internal static class ConfigurationSettingsHelper
{
    private const string SpringApplicationPrefix = "spring:application";
    private const string VcapApplicationPrefix = "vcap:application";
    private const string VcapServicesConfigserverPrefix = "vcap:services:p-config-server:0";
    private const string VcapServicesConfigserver30Prefix = "vcap:services:p.config-server:0";
    private const string VcapServicesConfigserverAltPrefix = "vcap:services:config-server:0";

    public static void Initialize(string sectionPrefix, ConfigServerClientSettings settings, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(sectionPrefix);
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection configurationSection = configuration.GetSection(sectionPrefix);

        settings.Name = GetApplicationName(sectionPrefix, configuration, settings.Name);
        settings.Environment = GetEnvironment(configurationSection, settings.Environment);
        settings.Label = configurationSection.GetValue("label", settings.Label);
        settings.Username = configurationSection.GetValue("username", settings.Username);
        settings.Password = configurationSection.GetValue("password", settings.Password);
        settings.Uri = configurationSection.GetValue("uri", settings.Uri);
        settings.Enabled = configurationSection.GetValue("enabled", settings.Enabled);
        settings.FailFast = configurationSection.GetValue("failFast", settings.FailFast);
        settings.ValidateCertificates = GetCertificateValidation(configurationSection, settings.ValidateCertificates);
        settings.RetryEnabled = configurationSection.GetValue("retry:enabled", settings.RetryEnabled);
        settings.RetryInitialInterval = configurationSection.GetValue("retry:initialInterval", settings.RetryInitialInterval);
        settings.RetryMaxInterval = configurationSection.GetValue("retry:maxInterval", settings.RetryMaxInterval);
        settings.RetryMultiplier = configurationSection.GetValue("retry:multiplier", settings.RetryMultiplier);
        settings.RetryAttempts = configurationSection.GetValue("retry:maxAttempts", settings.RetryAttempts);
        settings.Token = configurationSection.GetValue<string>("token");
        settings.Timeout = configurationSection.GetValue("timeout", settings.Timeout);
        settings.AccessTokenUri = GetAccessTokenUri(sectionPrefix, configuration);
        settings.ClientId = GetClientId(sectionPrefix, configuration);
        settings.ClientSecret = GetClientSecret(sectionPrefix, configuration);
        settings.TokenRenewRate = configurationSection.GetValue("tokenRenewRate", ConfigServerClientSettings.DefaultVaultTokenRenewRate);
        settings.DisableTokenRenewal = configurationSection.GetValue("disableTokenRenewal", ConfigServerClientSettings.DefaultDisableTokenRenewal);
        settings.TokenTtl = configurationSection.GetValue("tokenTtl", ConfigServerClientSettings.DefaultVaultTokenTtl);
        settings.DiscoveryEnabled = configurationSection.GetValue("discovery:enabled", settings.DiscoveryEnabled);
        settings.DiscoveryServiceId = configurationSection.GetValue("discovery:serviceId", settings.DiscoveryServiceId);
        settings.HealthEnabled = configurationSection.GetValue("health:enabled", settings.HealthEnabled);
        settings.HealthTimeToLive = configurationSection.GetValue("health:timeToLive", settings.HealthTimeToLive);
        settings.PollingInterval = configurationSection.GetValue("pollingInterval", settings.PollingInterval);

        // Override Config Server URI
        settings.Uri = GetCloudFoundryUri(sectionPrefix, configuration, settings.Uri);
    }

    private static string GetEnvironment(IConfigurationSection section, string defaultValue)
    {
        return section.GetValue("env", string.IsNullOrEmpty(defaultValue) ? ConfigServerClientSettings.DefaultEnvironment : defaultValue);
    }

    private static bool GetCertificateValidation(IConfigurationSection section, bool defaultValue)
    {
        return section.GetValue("validateCertificates", defaultValue) && section.GetValue("validate_certificates", defaultValue);
    }

    private static string GetClientSecret(string sectionPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:client_secret", configuration, ConfigServerClientSettings.DefaultClientSecret,
            VcapServicesConfigserverPrefix, VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, sectionPrefix);
    }

    private static string GetClientId(string sectionPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:client_id", configuration, ConfigServerClientSettings.DefaultClientId,
            VcapServicesConfigserverPrefix, VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, sectionPrefix);
    }

    private static string GetAccessTokenUri(string sectionPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:access_token_uri", configuration, ConfigServerClientSettings.DefaultAccessTokenUri,
            VcapServicesConfigserverPrefix, VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, sectionPrefix);
    }

    private static string GetApplicationName(string sectionPrefix, IConfiguration configuration, string defaultValue)
    {
        return ConfigurationValuesHelper.GetSetting("name", configuration, defaultValue, sectionPrefix, SpringApplicationPrefix, VcapApplicationPrefix);
    }

    private static string GetCloudFoundryUri(string sectionPrefix, IConfiguration configuration, string defaultValue)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:uri", configuration, defaultValue, sectionPrefix, VcapServicesConfigserverPrefix,
            VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix);
    }
}
