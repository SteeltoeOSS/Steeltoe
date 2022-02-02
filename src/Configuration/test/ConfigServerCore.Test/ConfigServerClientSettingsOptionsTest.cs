// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.ConfigServer.Test;
using System.IO;
using System.Text;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerClientSettingsOptionsTest
    {
        [Fact]
        public void ConfigureConfigServerClientSettingsOptions_WithDefaults()
        {
            var services = new ServiceCollection().AddOptions();
            var environment = HostingHelpers.GetHostingEnvironment("Production");

            var builder = new ConfigurationBuilder().AddConfigServer(environment);
            var config = builder.Build();

            services.ConfigureConfigServerClientOptions(config);
            var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            TestHelper.VerifyDefaults(options.Settings);

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
            Assert.Equal(ConfigServerClientSettings.DEFAULT_DISCOVERY_ENABLED, options.DiscoveryEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CONFIGSERVER_SERVICEID, options.DiscoveryServiceId);
            Assert.Null(options.Name);
            Assert.Null(options.Label);
            Assert.Null(options.Username);
            Assert.Null(options.Password);
            Assert.Null(options.Token);
            Assert.Null(options.Headers);
        }

        [Fact]
        public void ConfigureConfigServerClientSettingsOptions_WithValues()
        {
            var services = new ServiceCollection().AddOptions();
            var environment = HostingHelpers.GetHostingEnvironment("Production");
            var appsettings = @"
                {
                    ""spring"": {
                      ""application"": {
                        ""name"": ""foo""
                      },
                      ""cloud"": {
                        ""config"": {
                            ""uri"": ""http://localhost:8888"",
                            ""env"": ""development"",
                            ""headers"" : {
                                ""foo"":""bar"",
                                ""bar"":""foo""
                            },
                            ""health"": {
                                ""enabled"": true
                            },
                            ""failfast"": ""true""
                        }
                      }
                    }
                }";
            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(directory);
            builder.AddJsonFile(fileName);
            var config = builder.Build();

            services.ConfigureConfigServerClientOptions(config);
            var service = services.BuildServiceProvider().GetService<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);

            Assert.Equal(ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED, options.Enabled);
            Assert.True(options.FailFast);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_URI, options.Uri);
            Assert.Equal("development", options.Environment);
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
            Assert.Equal(ConfigServerClientSettings.DEFAULT_DISCOVERY_ENABLED, options.DiscoveryEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CONFIGSERVER_SERVICEID, options.DiscoveryServiceId);
            Assert.Null(options.Name);
            Assert.Null(options.Label);
            Assert.Null(options.Username);
            Assert.Null(options.Password);
            Assert.Null(options.Token);
            Assert.NotNull(options.Headers);
            Assert.Equal("foo", options.Headers["bar"]);
            Assert.Equal("bar", options.Headers["foo"]);

            var settings = options.Settings;
            Assert.NotNull(settings);

            Assert.Equal(ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED, settings.Enabled);
            Assert.True(settings.FailFast);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_URI, settings.Uri);
            Assert.Equal("development", settings.Environment);
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
            Assert.NotNull(settings.Headers);
            Assert.Equal("foo", options.Headers["bar"]);
            Assert.Equal("bar", options.Headers["foo"]);
        }
    }
}
