// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public static class ConfigurationSettingsHelper
{
    private const string SpringApplicationPrefix = "spring:application";
    private const string VcapApplicationPrefix = "vcap:application";
    private const string VcapServicesConfigserverPrefix = "vcap:services:p-config-server:0";
    private const string VcapServicesConfigserver30Prefix = "vcap:services:p.config-server:0";
    private const string VcapServicesConfigserverAltPrefix = "vcap:services:config-server:0";

    public static void Initialize(string configPrefix, ConfigServerClientSettings settings, IConfiguration config)
    {
        ArgumentGuard.NotNull(configPrefix);
        ArgumentGuard.NotNull(settings);
        ArgumentGuard.NotNull(config);

        IConfigurationSection configurationSection = config.GetSection(configPrefix);

        settings.Name = GetApplicationName(configPrefix, config, settings.Name);
        settings.Environment = GetEnvironment(configurationSection, settings.Environment);
        settings.Label = GetLabel(configurationSection, settings.Label);
        settings.Username = GetUsername(configurationSection, settings.Username);
        settings.Password = GetPassword(configurationSection, settings.Password);
        settings.Uri = GetUri(configurationSection, settings.Uri);
        settings.Enabled = GetEnabled(configurationSection, settings.Enabled);
        settings.FailFast = GetFailFast(configurationSection, settings.FailFast);
        settings.ValidateCertificates = GetCertificateValidation(configurationSection, settings.ValidateCertificates);
        settings.RetryEnabled = GetRetryEnabled(configurationSection, settings.RetryEnabled);
        settings.RetryInitialInterval = GetRetryInitialInterval(configurationSection, settings.RetryInitialInterval);
        settings.RetryMaxInterval = GetRetryMaxInterval(configurationSection, settings.RetryMaxInterval);
        settings.RetryMultiplier = GetRetryMultiplier(configurationSection, settings.RetryMultiplier);
        settings.RetryAttempts = GetRetryMaxAttempts(configurationSection, settings.RetryAttempts);
        settings.Token = GetToken(configurationSection);
        settings.Timeout = GetTimeout(configurationSection, settings.Timeout);
        settings.AccessTokenUri = GetAccessTokenUri(configPrefix, config);
        settings.ClientId = GetClientId(configPrefix, config);
        settings.ClientSecret = GetClientSecret(configPrefix, config);
        settings.TokenRenewRate = GetTokenRenewRate(configurationSection);
        settings.DisableTokenRenewal = GetDisableTokenRenewal(configurationSection);
        settings.TokenTtl = GetTokenTtl(configurationSection);
        settings.DiscoveryEnabled = GetDiscoveryEnabled(configurationSection, settings.DiscoveryEnabled);
        settings.DiscoveryServiceId = GetDiscoveryServiceId(configurationSection, settings.DiscoveryServiceId);
        settings.HealthEnabled = GetHealthEnabled(configurationSection, settings.HealthEnabled);
        settings.HealthTimeToLive = GetHealthTimeToLive(configurationSection, settings.HealthTimeToLive);
        settings.PollingInterval = GetPollingInterval(configurationSection, settings.PollingInterval);

        // Override Config server URI
        settings.Uri = GetCloudFoundryUri(configPrefix, config, settings.Uri);
    }

    private static bool GetHealthEnabled(IConfigurationSection configurationSection, bool def)
    {
        return configurationSection.GetValue("health:enabled", def);
    }

    private static long GetHealthTimeToLive(IConfigurationSection configurationSection, long def)
    {
        return configurationSection.GetValue("health:timeToLive", def);
    }

    private static bool GetDiscoveryEnabled(IConfigurationSection configurationSection, bool def)
    {
        return configurationSection.GetValue("discovery:enabled", def);
    }

    private static string GetDiscoveryServiceId(IConfigurationSection configurationSection, string def)
    {
        return configurationSection.GetValue("discovery:serviceId", def);
    }

    private static int GetRetryMaxAttempts(IConfigurationSection configurationSection, int def)
    {
        return configurationSection.GetValue("retry:maxAttempts", def);
    }

    private static double GetRetryMultiplier(IConfigurationSection configurationSection, double def)
    {
        return configurationSection.GetValue("retry:multiplier", def);
    }

    private static int GetRetryMaxInterval(IConfigurationSection configurationSection, int def)
    {
        return configurationSection.GetValue("retry:maxInterval", def);
    }

    private static int GetRetryInitialInterval(IConfigurationSection configurationSection, int def)
    {
        return configurationSection.GetValue("retry:initialInterval", def);
    }

    private static bool GetRetryEnabled(IConfigurationSection configurationSection, bool def)
    {
        return configurationSection.GetValue("retry:enabled", def);
    }

    private static bool GetFailFast(IConfigurationSection configurationSection, bool def)
    {
        return configurationSection.GetValue("failFast", def);
    }

    private static bool GetEnabled(IConfigurationSection configurationSection, bool def)
    {
        return configurationSection.GetValue("enabled", def);
    }

    private static string GetToken(IConfigurationSection configurationSection)
    {
        return configurationSection.GetValue<string>("token");
    }

    private static int GetTimeout(IConfigurationSection configurationSection, int def)
    {
        return configurationSection.GetValue("timeout", def);
    }

    private static string GetUri(IConfigurationSection configurationSection, string def)
    {
        return configurationSection.GetValue("uri", def);
    }

    private static string GetPassword(IConfigurationSection configurationSection, string defaultPassword)
    {
        return configurationSection.GetValue("password", defaultPassword);
    }

    private static string GetUsername(IConfigurationSection configurationSection, string defaultUser)
    {
        return configurationSection.GetValue("username", defaultUser);
    }

    private static string GetLabel(IConfigurationSection configurationSection, string defaultLabel)
    {
        return configurationSection.GetValue("label", defaultLabel);
    }

    private static string GetEnvironment(IConfigurationSection configurationSection, string def)
    {
        return configurationSection.GetValue("env", string.IsNullOrEmpty(def) ? ConfigServerClientSettings.DefaultEnvironment : def);
    }

    private static bool GetCertificateValidation(IConfigurationSection configurationSection, bool def)
    {
        return configurationSection.GetValue("validateCertificates", def) && configurationSection.GetValue("validate_certificates", def);
    }

    private static int GetTokenRenewRate(IConfigurationSection configurationSection)
    {
        return configurationSection.GetValue("tokenRenewRate", ConfigServerClientSettings.DefaultVaultTokenRenewRate);
    }

    private static bool GetDisableTokenRenewal(IConfigurationSection configurationSection)
    {
        return configurationSection.GetValue("disableTokenRenewal", ConfigServerClientSettings.DefaultDisableTokenRenewal);
    }

    private static int GetTokenTtl(IConfigurationSection configurationSection)
    {
        return configurationSection.GetValue("tokenTtl", ConfigServerClientSettings.DefaultVaultTokenTtl);
    }

    private static TimeSpan GetPollingInterval(IConfigurationSection configurationSection, TimeSpan def)
    {
        return configurationSection.GetValue("pollingInterval", def);
    }

    private static string GetClientSecret(string configPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:client_secret", configuration, ConfigServerClientSettings.DefaultClientSecret,
            VcapServicesConfigserverPrefix, VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, configPrefix);
    }

    private static string GetClientId(string configPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:client_id", configuration, ConfigServerClientSettings.DefaultClientId,
            VcapServicesConfigserverPrefix, VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, configPrefix);
    }

    private static string GetAccessTokenUri(string configPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:access_token_uri", configuration, ConfigServerClientSettings.DefaultAccessTokenUri,
            VcapServicesConfigserverPrefix, VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, configPrefix);
    }

    private static string GetApplicationName(string configPrefix, IConfiguration configuration, string defName)
    {
        return ConfigurationValuesHelper.GetSetting("name", configuration, defName, configPrefix, SpringApplicationPrefix, VcapApplicationPrefix);
    }

    private static string GetCloudFoundryUri(string configPrefix, IConfiguration configuration, string def)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:uri", configuration, def, configPrefix, VcapServicesConfigserverPrefix,
            VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix);
    }
}
