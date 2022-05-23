// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using Xunit;

namespace Steeltoe.Connector.OAuth.Test
{
    public class OAuthServiceCollectionExtensionsTest
    {
        public OAuthServiceCollectionExtensionsTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void AddOAuthServiceOptions_ThrowsIfServiceCollectionNull()
        {
            IServiceCollection services = null;
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOAuthServiceOptions(config));
            Assert.Contains(nameof(services), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOAuthServiceOptions(config, "foobar"));
            Assert.Contains(nameof(services), ex2.Message);
        }

        [Fact]
        public void AddOAuthServiceOptions_ThrowsIfConfigurationNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOAuthServiceOptions(config));
            Assert.Contains(nameof(config), ex.Message);

            var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOAuthServiceOptions(config, "foobar"));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddOAuthServiceOptions_ThrowsIfServiceNameNull()
        {
            IServiceCollection services = new ServiceCollection();
            IConfigurationRoot config = null;
            string serviceName = null;

            var ex = Assert.Throws<ArgumentNullException>(() => services.AddOAuthServiceOptions(config, serviceName));
            Assert.Contains(nameof(serviceName), ex.Message);
        }

        [Fact]
        public void AddOAuthServiceOptions_NoVCAPs_AddsOAuthOptions()
        {
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            services.AddOAuthServiceOptions(config);

            var service = services.BuildServiceProvider().GetService<IOptions<OAuthServiceOptions>>();
            Assert.NotNull(service);
        }

        [Fact]
        public void AddOAuthServiceOptions_WithServiceName_NoVCAPs_ThrowsConnectorException()
        {
            IServiceCollection services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();

            var ex = Assert.Throws<ConnectorException>(() => services.AddOAuthServiceOptions(config, "foobar"));
            Assert.Contains("foobar", ex.Message);
        }

        [Fact]
        public void AddOAuthServiceOptions_MultipleOAuthServices_ThrowsConnectorException()
        {
            var env2 = @"
                {
                    ""p-identity"": [{
                        ""credentials"": {
                            ""client_id"": ""cb3efc76-bd22-46b3-a5ca-3aaa21c96073"",
                            ""client_secret"": ""92b5ebf0-c67b-4671-98d3-8e316fb11e30"",
                            ""auth_domain"": ""https://sso.login.system.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-identity"",
                        ""provider"": null,
                        ""plan"": ""sso"",
                        ""name"": ""mySSO"",
                        ""tags"": []
                    },
                    {
                        ""credentials"": {
                            ""client_id"": ""cb3efc76-bd22-46b3-a5ca-3aaa21c96073"",
                            ""client_secret"": ""92b5ebf0-c67b-4671-98d3-8e316fb11e30"",
                            ""auth_domain"": ""https://sso.login.system.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-identity"",
                        ""provider"": null,
                        ""plan"": ""sso"",
                        ""name"": ""mySSO2"",
                        ""tags"": []
                    }]
                }";

            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            var ex = Assert.Throws<ConnectorException>(() => services.AddOAuthServiceOptions(config));
            Assert.Contains("Multiple", ex.Message);
        }

        [Fact]
        public void AddOAuthServiceOptions_WithVCAPs_AddsOAuthOptions()
        {
            var env2 = @"
                {
                    ""p-identity"": [{
                        ""credentials"": {
                            ""client_id"": ""cb3efc76-bd22-46b3-a5ca-3aaa21c96073"",
                            ""client_secret"": ""92b5ebf0-c67b-4671-98d3-8e316fb11e30"",
                            ""auth_domain"": ""https://sso.login.system.testcloud.com""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-identity"",
                        ""provider"": null,
                        ""plan"": ""sso"",
                        ""name"": ""mySSO"",
                        ""tags"": []
                    }]
                }";

            IServiceCollection services = new ServiceCollection();

            Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", env2);

            var builder = new ConfigurationBuilder();
            builder.AddCloudFoundry();
            var config = builder.Build();

            services.AddOAuthServiceOptions(config);

            var service = services.BuildServiceProvider().GetService<IOptions<OAuthServiceOptions>>();
            Assert.NotNull(service);

            var opts = service.Value;
            Assert.NotNull(opts);

            Assert.Equal("cb3efc76-bd22-46b3-a5ca-3aaa21c96073", opts.ClientId);
            Assert.Equal("92b5ebf0-c67b-4671-98d3-8e316fb11e30", opts.ClientSecret);
            Assert.Equal($"https://sso.login.system.testcloud.com{OAuthConnectorDefaults.Default_AccessTokenUri}", opts.AccessTokenUrl);
            Assert.Equal($"https://sso.login.system.testcloud.com{OAuthConnectorDefaults.Default_JwtTokenKey}", opts.JwtKeyUrl);
            Assert.Equal($"https://sso.login.system.testcloud.com{OAuthConnectorDefaults.Default_CheckTokenUri}", opts.TokenInfoUrl);
            Assert.Equal($"https://sso.login.system.testcloud.com{OAuthConnectorDefaults.Default_AuthorizationUri}", opts.UserAuthorizationUrl);
            Assert.Equal($"https://sso.login.system.testcloud.com{OAuthConnectorDefaults.Default_UserInfoUri}", opts.UserInfoUrl);
            Assert.NotNull(opts.Scope);
            Assert.Equal(0, opts.Scope.Count);
        }
    }
}
