// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Extensions.Configuration.ConfigServer;
using Steeltoe.Extensions.Configuration.ConfigServer.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class ConfigServerServiceCollectionExtensionsTest
    {
        [Fact]
        public void ConfigureConfigServerClientOptions_ThrowsIfServiceCollectionNull()
        {
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureConfigServerClientOptions(config));
            Assert.Contains(nameof(services), ex.Message);
        }

        [Fact]
        public void ConfigureConfigServerClientOptions_ThrowsIfConfigurationNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureConfigServerClientOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void ConfigureConfigServerClientOptions_ConfiguresConfigServerClientSettingsOptions_WithDefaults()
        {
            var services = new ServiceCollection();
            var environment = HostingHelpers.GetHostingEnvironment("Production");

            var builder = new ConfigurationBuilder().AddConfigServer(environment);
            var config = builder.Build();
            services.ConfigureConfigServerClientOptions(config);

            var serviceProvider = services.BuildServiceProvider();
            var service = serviceProvider.GetService<IOptions<ConfigServerClientSettingsOptions>>();
            Assert.NotNull(service);
            var options = service.Value;
            Assert.NotNull(options);
            TestHelper.VerifyDefaults(options.Settings);

            Assert.Equal(ConfigServerClientSettings.DEFAULT_PROVIDER_ENABLED, options.Enabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_FAILFAST, options.FailFast);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_URI, options.Uri);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_ENVIRONMENT, options.Environment);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_CERTIFICATE_VALIDATION, options.ValidateCertificates);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_INITIAL_RETRY_INTERVAL, options.RetryInitialInterval);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_MAX_RETRY_ATTEMPTS, options.RetryAttempts);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_RETRY_ENABLED, options.RetryEnabled);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_RETRY_MULTIPLIER, options.RetryMultiplier);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_MAX_RETRY_INTERVAL, options.RetryMaxInterval);
            Assert.Equal(ConfigServerClientSettings.DEFAULT_TIMEOUT_MILLISECONDS, options.Timeout);
            Assert.Null(options.Name);
            Assert.Null(options.Label);
            Assert.Null(options.Username);
            Assert.Null(options.Password);
            Assert.Null(options.Token);
            Assert.Null(options.AccessTokenUri);
            Assert.Null(options.ClientId);
            Assert.Null(options.ClientSecret);
            Assert.Null(options.Headers);
        }

        [Fact]
        public void ConfigureConfigServerClientOptions_ConfiguresCloudFoundryOptions()
        {
            var services = new ServiceCollection();
            var environment = HostingHelpers.GetHostingEnvironment();

            var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:cloud:config:timeout", "10" } }).AddConfigServer(environment);
            var config = builder.Build();
            services.ConfigureConfigServerClientOptions(config);

            var serviceProvider = services.BuildServiceProvider();
            var app = serviceProvider.GetService<IOptions<CloudFoundryApplicationOptions>>();
            Assert.NotNull(app);
            var service = serviceProvider.GetService<IOptions<CloudFoundryServicesOptions>>();
            Assert.NotNull(service);
        }
    }
}
