﻿// Copyright 2017 the original author or authors.
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

using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class TestHelper
    {
        public static void VerifyDefaults(ConfigServerClientSettings settings)
        {
            Assert.Equal(ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED, settings.Enabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_FAILFAST, settings.FailFast);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_URI, settings.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, settings.Environment);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ACCESS_TOKEN_URI, settings.AccessTokenUri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CLIENT_ID, settings.ClientId);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CLIENT_SECRET, settings.ClientSecret);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION, settings.ValidateCertificates);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_INITIAL_RETRY_INTERVAL, settings.RetryInitialInterval);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_MAX_RETRY_ATTEMPTS, settings.RetryAttempts);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_RETRY_ENABLED, settings.RetryEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_RETRY_MULTIPLIER, settings.RetryMultiplier);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_MAX_RETRY_INTERVAL, settings.RetryMaxInterval);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_TIMEOUT_MILLISECONDS, settings.Timeout);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_RENEW_RATE, settings.TokenRenewRate);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_TTL, settings.TokenTtl);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_DISCOVERY_ENABLED, settings.DiscoveryEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CONFIGSERVER_SERVICEID, settings.DiscoveryServiceId);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_HEALTH_ENABLED, settings.HealthEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_HEALTH_TIMETOLIVE, settings.HealthTimeToLive);
            Assert.Null(settings.Name);
            Assert.Null(settings.Label);
            Assert.Null(settings.Username);
            Assert.Null(settings.Password);
            Assert.Null(settings.Token);
        }
    }
}
