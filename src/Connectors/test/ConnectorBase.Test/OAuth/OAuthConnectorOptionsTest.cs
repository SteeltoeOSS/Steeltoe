// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.OAuth.Test
{
    public class OAuthConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            IConfiguration config = null;

            var ex = Assert.Throws<ArgumentNullException>(() => new OAuthConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>
            {
                ["security:oauth2:client:oauthServiceUrl"] = "https://foo.bar",
                ["security:oauth2:client:clientid"] = "clientid",
                ["security:oauth2:client:clientSecret"] = "clientsecret",
                ["security:oauth2:client:userAuthorizationUri"] = "userauthorizationuri",
                ["security:oauth2:client:accessTokenUri"] = "accesstokenuri",
                ["security:oauth2:client:scope:0"] = "foo",
                ["security:oauth2:client:scope:1"] = "bar",
                ["security:oauth2:resource:userInfoUri"] = "userinfouri",
                ["security:oauth2:resource:tokenInfoUri"] = "tokeninfouri",
                ["security:oauth2:resource:jwtKeyUri"] = "jwtkeyuri"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new OAuthConnectorOptions(config);
            Assert.Equal("accesstokenuri", sconfig.AccessTokenUri);
            Assert.Equal("clientid", sconfig.ClientId);
            Assert.Equal("clientsecret", sconfig.ClientSecret);
            Assert.Equal("jwtkeyuri", sconfig.JwtKeyUri);
            Assert.Equal("https://foo.bar", sconfig.OAuthServiceUrl);
            Assert.Equal("tokeninfouri", sconfig.TokenInfoUri);
            Assert.Equal("userauthorizationuri", sconfig.UserAuthorizationUri);
            Assert.Equal("userinfouri", sconfig.UserInfoUri);
            Assert.NotNull(sconfig.Scope);
            Assert.Equal(2, sconfig.Scope.Count);
            Assert.True(sconfig.Scope.Contains("foo") && sconfig.Scope.Contains("bar"));
            Assert.True(sconfig.ValidateCertificates);
        }

        [Fact]
        public void ValidateCertificates_Binds()
        {
            // arrange a configuration with validateCertificates=false
            var appsettings = new Dictionary<string, string>
            {
                ["security:oauth2:client:validateCertificates"] = "false",
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new OAuthConnectorOptions(config);

            Assert.False(sconfig.ValidateCertificates);
        }

        [Fact]
        public void Validate_Certificates_Binds()
        {
            // arrange a configuration with validateCertificates=false
            var appsettings = new Dictionary<string, string>
            {
                ["security:oauth2:client:validate_certificates"] = "false",
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new OAuthConnectorOptions(config);

            Assert.False(sconfig.ValidateCertificates);
        }
    }
}
