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
using Steeltoe.Common.Options;

namespace Steeltoe.Extensions.Configuration.ConfigServer
{
    public class ConfigServerClientSettingsOptions : AbstractOptions
    {
        public const string CONFIGURATION_PREFIX = "spring:cloud:config";

        public ConfigServerClientSettingsOptions()
        {
        }

        public ConfigServerClientSettingsOptions(IConfigurationRoot root)
            : base(root, CONFIGURATION_PREFIX)
        {
        }

        public ConfigServerClientSettingsOptions(IConfiguration config)
            : base(config)
        {
        }

        public bool Enabled { get; set; } = ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED;

        public bool FailFast { get; set; } = ConfigServerClientSettings.DEFAULT_FAILFAST;

        public string Env { get; set; }

        public string Label { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public string Uri { get; set; }

        public string Username { get; set; }

        public string Token { get; set; }

        public int Timeout { get; set; } = ConfigServerClientSettings.DEFAULT_TIMEOUT_MILLISECONDS;

        public bool Validate_Certificates { get; set; } = ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION;

        public SpringCloudConfigRetry Retry { get; set; }

        public SpringCloudConfigDiscovery Discovery { get; set; }

        public SpringCloudConfigHealth Health { get; set; }

        public bool ValidateCertificates => Validate_Certificates;

        public string Environment => Env;

        public bool RetryEnabled => Retry != null ? Retry.Enabled : ConfigServerClientSettings.DEFAULT_RETRY_ENABLED;

        public int RetryInitialInterval => Retry != null ? Retry.InitialInterval : ConfigServerClientSettings.DEFAULT_INITIAL_RETRY_INTERVAL;

        public int RetryMaxInterval => Retry != null ? Retry.MaxInterval : ConfigServerClientSettings.DEFAULT_MAX_RETRY_INTERVAL;

        public double RetryMultiplier => Retry != null ? Retry.Multiplier : ConfigServerClientSettings.DEFAULT_RETRY_MULTIPLIER;

        public int RetryAttempts => Retry != null ? Retry.MaxAttempts : ConfigServerClientSettings.DEFAULT_MAX_RETRY_ATTEMPTS;

        public bool DiscoveryEnabled => Discovery != null ? Discovery.Enabled : ConfigServerClientSettings.DEFAULT_DISCOVERY_ENABLED;

        public string DiscoveryServiceId => Discovery != null ? Discovery.ServiceId : ConfigServerClientSettings.DEFAULT_CONFIGSERVER_SERVICEID;

        public bool HealthEnabled => Health != null ? Health.Enabled : ConfigServerClientSettings.DEFAULT_HEALTH_ENABLED;

        public long HealthTimeToLive => Health != null ? Health.TimeToLive : ConfigServerClientSettings.DEFAULT_HEALTH_TIMETOLIVE;

        public string Access_Token_Uri { get; set; }

        public string Client_Secret { get; set; }

        public string Client_Id { get; set; }

        public int TokenTtl { get; set; } = ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_TTL;

        public int TokenRenewRate { get; set; } = ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_RENEW_RATE;

        public bool DisableTokenRenewal { get; set; } = ConfigServerClientSettings.DEFAULT_DISABLE_TOKEN_RENEWAL;

        public string AccessTokenUri => Access_Token_Uri;

        public string ClientSecret => Client_Secret;

        public string ClientId => Client_Id;

        public ConfigServerClientSettings Settings
        {
            get
            {
                var settings = new ConfigServerClientSettings();

                settings.Enabled = Enabled;
                settings.FailFast = FailFast;
                settings.ValidateCertificates = Validate_Certificates;
                settings.RetryAttempts = RetryAttempts;
                settings.RetryEnabled = RetryEnabled;
                settings.RetryInitialInterval = RetryInitialInterval;
                settings.RetryMaxInterval = RetryMaxInterval;
                settings.RetryMultiplier = RetryMultiplier;
                settings.Timeout = Timeout;
                settings.TokenTtl = TokenTtl;
                settings.TokenRenewRate = TokenRenewRate;
                settings.DisableTokenRenewal = DisableTokenRenewal;

                settings.Environment = Env;
                settings.Label = Label;
                settings.Name = Name;
                settings.Password = Password;
                settings.Uri = Uri;
                settings.Username = Username;
                settings.Token = Token;
                settings.AccessTokenUri = Access_Token_Uri;
                settings.ClientSecret = Client_Secret;
                settings.ClientId = Client_Id;

                settings.DiscoveryEnabled = DiscoveryEnabled;
                settings.DiscoveryServiceId = DiscoveryServiceId;

                settings.HealthEnabled = HealthEnabled;
                settings.HealthTimeToLive = HealthTimeToLive;

                return settings;
            }
        }
    }
}
