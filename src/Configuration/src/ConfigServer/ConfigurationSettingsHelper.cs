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

    public static void Initialize(string sectionPrefix, ConfigServerClientOptions options, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(sectionPrefix);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(configuration);

        IConfigurationSection configurationSection = configuration.GetSection(sectionPrefix);

        options.Name = GetApplicationName(sectionPrefix, configuration, options.Name);
        options.Environment = GetEnvironment(configurationSection, options.Environment);
        options.Label = configurationSection.GetValue("label", options.Label);
        options.Username = configurationSection.GetValue("username", options.Username);
        options.Password = configurationSection.GetValue("password", options.Password);
        options.Uri = configurationSection.GetValue("uri", options.Uri);
        options.Enabled = configurationSection.GetValue("enabled", options.Enabled);
        options.FailFast = configurationSection.GetValue("failFast", options.FailFast);
        options.ValidateCertificates = GetCertificateValidation(configurationSection, options.ValidateCertificates);
        options.Retry.Enabled = configurationSection.GetValue("retry:enabled", options.Retry.Enabled);
        options.Retry.InitialInterval = configurationSection.GetValue("retry:initialInterval", options.Retry.InitialInterval);
        options.Retry.MaxInterval = configurationSection.GetValue("retry:maxInterval", options.Retry.MaxInterval);
        options.Retry.Multiplier = configurationSection.GetValue("retry:multiplier", options.Retry.Multiplier);
        options.Retry.MaxAttempts = configurationSection.GetValue("retry:maxAttempts", options.Retry.MaxAttempts);
        options.Token = configurationSection.GetValue("token", options.Token);
        options.Timeout = configurationSection.GetValue("timeout", options.Timeout);
        options.AccessTokenUri = GetAccessTokenUri(sectionPrefix, configuration);
        options.ClientId = GetClientId(sectionPrefix, configuration);
        options.ClientSecret = GetClientSecret(sectionPrefix, configuration);
        options.TokenRenewRate = configurationSection.GetValue("tokenRenewRate", 60_000);
        options.DisableTokenRenewal = configurationSection.GetValue("disableTokenRenewal", false);
        options.TokenTtl = configurationSection.GetValue("tokenTtl", 300_000);
        options.Discovery.Enabled = configurationSection.GetValue("discovery:enabled", options.Discovery.Enabled);
        options.Discovery.ServiceId = configurationSection.GetValue("discovery:serviceId", options.Discovery.ServiceId);
        options.Health.Enabled = configurationSection.GetValue("health:enabled", options.Health.Enabled);
        options.Health.TimeToLive = configurationSection.GetValue("health:timeToLive", options.Health.TimeToLive);
        options.PollingInterval = configurationSection.GetValue("pollingInterval", options.PollingInterval);

        // Override Config Server URI
        options.Uri = GetCloudFoundryUri(sectionPrefix, configuration, options.Uri);
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
