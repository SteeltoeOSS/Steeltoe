// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerClientSettingsOptionsTest
    {
        [Fact]
        public void ConfigureConfigServerClientSettingsOptions_WithDefaults()
        {
            // Arrange
            var services = new ServiceCollection().AddOptions();
            var environment = new HostingEnvironment();

            // Act and Assert
            var builder = new ConfigurationBuilder().AddConfigServer(environment);
            var config = builder.Build();

            services.ConfigureConfigServerClientOptions(config);
            var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            TestHelpers.VerifyDefaults(options.Settings);

            Assert.Equal(ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED, options.Enabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_FAILFAST, options.FailFast);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_URI, options.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, options.Environment);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ACCESS_TOKEN_URI, options.AccessTokenUri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CLIENT_ID, options.ClientId);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CLIENT_SECRET, options.ClientSecret);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION, options.ValidateCertificates);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_INITIAL_RETRY_INTERVAL, options.RetryInitialInterval);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_MAX_RETRY_ATTEMPTS, options.RetryAttempts);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_RETRY_ENABLED, options.RetryEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_RETRY_MULTIPLIER, options.RetryMultiplier);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_MAX_RETRY_INTERVAL, options.RetryMaxInterval);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_TIMEOUT_MILLISECONDS, options.Timeout);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_RENEW_RATE, options.TokenRenewRate);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_VAULT_TOKEN_TTL, options.TokenTtl);
            Assert.Null(options.Name);
            Assert.Null(options.Label);
            Assert.Null(options.Username);
            Assert.Null(options.Password);
            Assert.Null(options.Token);
        }
    }
}
