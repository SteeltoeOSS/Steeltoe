// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Configuration.ConfigServer;

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
        settings.Retry.Enabled = configurationSection.GetValue("retry:enabled", settings.Retry.Enabled);
        settings.Retry.InitialInterval = configurationSection.GetValue("retry:initialInterval", settings.Retry.InitialInterval);
        settings.Retry.MaxInterval = configurationSection.GetValue("retry:maxInterval", settings.Retry.MaxInterval);
        settings.Retry.Multiplier = configurationSection.GetValue("retry:multiplier", settings.Retry.Multiplier);
        settings.Retry.Attempts = configurationSection.GetValue("retry:maxAttempts", settings.Retry.Attempts);
        settings.Token = configurationSection.GetValue("token", settings.Token);
        settings.Timeout = configurationSection.GetValue("timeout", settings.Timeout);
        settings.AccessTokenUri = GetAccessTokenUri(sectionPrefix, configuration);
        settings.ClientId = GetClientId(sectionPrefix, configuration);
        settings.ClientSecret = GetClientSecret(sectionPrefix, configuration);
        settings.TokenRenewRate = configurationSection.GetValue("tokenRenewRate", 60_000);
        settings.DisableTokenRenewal = configurationSection.GetValue("disableTokenRenewal", false);
        settings.TokenTtl = configurationSection.GetValue("tokenTtl", 300_000);
        settings.Discovery.Enabled = configurationSection.GetValue("discovery:enabled", settings.Discovery.Enabled);
        settings.Discovery.ServiceId = configurationSection.GetValue("discovery:serviceId", settings.Discovery.ServiceId);
        settings.Health.Enabled = configurationSection.GetValue("health:enabled", settings.Health.Enabled);
        settings.Health.TimeToLive = configurationSection.GetValue("health:timeToLive", settings.Health.TimeToLive);
        settings.PollingInterval = configurationSection.GetValue("pollingInterval", settings.PollingInterval);

        // Override Config Server URI
        settings.Uri = GetCloudFoundryUri(sectionPrefix, configuration, settings.Uri);
    }

    private static string? GetEnvironment(IConfigurationSection section, string? defaultValue)
    {
        return section.GetValue("env", string.IsNullOrEmpty(defaultValue) ? "Production" : defaultValue);
    }

    private static bool GetCertificateValidation(IConfigurationSection section, bool defaultValue)
    {
        return section.GetValue("validateCertificates", defaultValue) && section.GetValue("validate_certificates", defaultValue);
    }

    private static string GetClientSecret(string sectionPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:client_secret", configuration, null, VcapServicesConfigserverPrefix,
            VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, sectionPrefix);
    }

    private static string GetClientId(string sectionPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:client_id", configuration, null, VcapServicesConfigserverPrefix,
            VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, sectionPrefix);
    }

    private static string GetAccessTokenUri(string sectionPrefix, IConfiguration configuration)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:access_token_uri", configuration, null, VcapServicesConfigserverPrefix,
            VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix, sectionPrefix);
    }

    private static string? GetApplicationName(string sectionPrefix, IConfiguration configuration, string? defaultValue)
    {
        return ConfigurationValuesHelper.GetSetting("name", configuration, defaultValue, sectionPrefix, SpringApplicationPrefix, VcapApplicationPrefix);
    }

    private static string? GetCloudFoundryUri(string sectionPrefix, IConfiguration configuration, string? defaultValue)
    {
        return ConfigurationValuesHelper.GetSetting("credentials:uri", configuration, defaultValue, sectionPrefix, VcapServicesConfigserverPrefix,
            VcapServicesConfigserver30Prefix, VcapServicesConfigserverAltPrefix);
    }
}
