// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Options;

namespace Steeltoe.Configuration.ConfigServer;

public sealed class ConfigServerClientSettingsOptions : AbstractOptions
{
    internal const string ConfigurationPrefix = "spring:cloud:config";

    public bool Enabled { get; set; } = true;

    public bool FailFast { get; set; }

    public string? Env { get; set; }

    public string? Label { get; set; }

    public string? Name { get; set; }

    public string? Password { get; set; }

    public string? Uri { get; set; }

    public string? Username { get; set; }

    public string? Token { get; set; }

    public int Timeout { get; set; } = 60_000;

    // ReSharper disable once InconsistentNaming
    public bool Validate_Certificates { get; set; } = true;

    public SpringCloudConfigRetry? Retry { get; set; }

    public SpringCloudConfigDiscovery? Discovery { get; set; }

    public SpringCloudConfigHealth? Health { get; set; }

    public bool ValidateCertificates => Validate_Certificates;

    public string? Environment => Env;

    public bool RetryEnabled => Retry is { Enabled: true };

    public int RetryInitialInterval => Retry?.InitialInterval ?? 1000;

    public int RetryMaxInterval => Retry?.MaxInterval ?? 2000;

    public double RetryMultiplier => Retry?.Multiplier ?? 1.1;

    public int RetryAttempts => Retry?.MaxAttempts ?? 6;

    public bool DiscoveryEnabled => Discovery is { Enabled: true };

    public string? DiscoveryServiceId => Discovery != null ? Discovery.ServiceId : "configserver";

    public bool HealthEnabled => Health == null || Health.Enabled;

    public long HealthTimeToLive => Health?.TimeToLive ?? 300_000;

    // ReSharper disable once InconsistentNaming
    public string? Access_Token_Uri { get; set; }

    // ReSharper disable once InconsistentNaming
    public string? Client_Secret { get; set; }

    // ReSharper disable once InconsistentNaming
    public string? Client_Id { get; set; }

    public int TokenTtl { get; set; } = 300_000;

    public int TokenRenewRate { get; set; } = 60_000;

    public bool DisableTokenRenewal { get; set; }

    public string? AccessTokenUri => Access_Token_Uri;

    public string? ClientSecret => Client_Secret;

    public string? ClientId => Client_Id;

    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

    public ConfigServerClientSettings Settings
    {
        get
        {
            ConfigServerClientSettings settings = new()
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

            foreach ((string key, string value) in Headers)
            {
                settings.Headers[key] = value;
            }

            return settings;
        }
    }

    // This constructor is for use with IOptions.
    public ConfigServerClientSettingsOptions()
    {
    }

    public ConfigServerClientSettingsOptions(IConfigurationRoot root)
        : base(root, ConfigurationPrefix)
    {
    }

    public ConfigServerClientSettingsOptions(IConfiguration configuration)
        : base(configuration)
    {
    }
}
