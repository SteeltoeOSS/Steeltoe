// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public class ConfigServerClientSettingsOptions : AbstractOptions
{
    public const string ConfigurationPrefix = "spring:cloud:config";

    public ConfigServerClientSettingsOptions()
    {
    }

    public ConfigServerClientSettingsOptions(IConfigurationRoot root)
        : base(root, ConfigurationPrefix)
    {
    }

    public ConfigServerClientSettingsOptions(IConfiguration config)
        : base(config)
    {
    }

    public bool Enabled { get; set; } = ConfigServerClientSettings.DefaultProviderEnabled;

    public bool FailFast { get; set; } = ConfigServerClientSettings.DefaultFailFast;

    public string Env { get; set; }

    public string Label { get; set; }

    public string Name { get; set; }

    public string Password { get; set; }

    public string Uri { get; set; }

    public string Username { get; set; }

    public string Token { get; set; }

    public int Timeout { get; set; } = ConfigServerClientSettings.DefaultTimeoutMilliseconds;

    // ReSharper disable once InconsistentNaming
    public bool Validate_Certificates { get; set; } = ConfigServerClientSettings.DefaultCertificateValidation;

    public SpringCloudConfigRetry Retry { get; set; }

    public SpringCloudConfigDiscovery Discovery { get; set; }

    public SpringCloudConfigHealth Health { get; set; }

    public bool ValidateCertificates => Validate_Certificates;

    public string Environment => Env;

    public bool RetryEnabled => Retry != null && Retry.Enabled;

    public int RetryInitialInterval => Retry?.InitialInterval ?? ConfigServerClientSettings.DefaultInitialRetryInterval;

    public int RetryMaxInterval => Retry?.MaxInterval ?? ConfigServerClientSettings.DefaultMaxRetryInterval;

    public double RetryMultiplier => Retry?.Multiplier ?? ConfigServerClientSettings.DefaultRetryMultiplier;

    public int RetryAttempts => Retry?.MaxAttempts ?? ConfigServerClientSettings.DefaultMaxRetryAttempts;

    public bool DiscoveryEnabled => Discovery != null && Discovery.Enabled;

    public string DiscoveryServiceId => Discovery != null ? Discovery.ServiceId : ConfigServerClientSettings.DefaultConfigserverServiceId;

    public bool HealthEnabled => Health == null || Health.Enabled;

    public long HealthTimeToLive => Health?.TimeToLive ?? ConfigServerClientSettings.DefaultHealthTimeToLive;

    // ReSharper disable once InconsistentNaming
    public string Access_Token_Uri { get; set; }

    // ReSharper disable once InconsistentNaming
    public string Client_Secret { get; set; }

    // ReSharper disable once InconsistentNaming
    public string Client_Id { get; set; }

    public int TokenTtl { get; set; } = ConfigServerClientSettings.DefaultVaultTokenTtl;

    public int TokenRenewRate { get; set; } = ConfigServerClientSettings.DefaultVaultTokenRenewRate;

    public bool DisableTokenRenewal { get; set; } = ConfigServerClientSettings.DefaultDisableTokenRenewal;

    public string AccessTokenUri => Access_Token_Uri;

    public string ClientSecret => Client_Secret;

    public string ClientId => Client_Id;

    public Dictionary<string, string> Headers { get; set; }

    public ConfigServerClientSettings Settings
    {
        get
        {
            var settings = new ConfigServerClientSettings
            {
                Enabled = Enabled,
                FailFast = FailFast,
                ValidateCertificates = Validate_Certificates,
                RetryAttempts = RetryAttempts,
                RetryEnabled = RetryEnabled,
                RetryInitialInterval = RetryInitialInterval,
                RetryMaxInterval = RetryMaxInterval,
                RetryMultiplier = RetryMultiplier,
                Timeout = Timeout,
                TokenTtl = TokenTtl,
                TokenRenewRate = TokenRenewRate,
                DisableTokenRenewal = DisableTokenRenewal,
                Headers = Headers,

                Environment = Env,
                Label = Label,
                Name = Name,
                Password = Password,
                Uri = Uri,
                Username = Username,
                Token = Token,
                AccessTokenUri = Access_Token_Uri,
                ClientSecret = Client_Secret,
                ClientId = Client_Id,

                DiscoveryEnabled = DiscoveryEnabled,
                DiscoveryServiceId = DiscoveryServiceId,

                HealthEnabled = HealthEnabled,
                HealthTimeToLive = HealthTimeToLive
            };

            return settings;
        }
    }
}
