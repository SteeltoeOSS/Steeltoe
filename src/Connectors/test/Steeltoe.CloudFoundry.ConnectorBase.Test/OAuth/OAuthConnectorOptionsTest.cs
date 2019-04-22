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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.OAuth.Test
{
    public class OAuthConnectorOptionsTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new OAuthConnectorOptions(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Constructor_BindsValues()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["security:oauth2:client:oauthServiceUrl"] = "http://foo.bar",
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

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            var sconfig = new OAuthConnectorOptions(config);
            Assert.Equal("accesstokenuri", sconfig.AccessTokenUri);
            Assert.Equal("clientid", sconfig.ClientId);
            Assert.Equal("clientsecret", sconfig.ClientSecret);
            Assert.Equal("jwtkeyuri", sconfig.JwtKeyUri);
            Assert.Equal("http://foo.bar", sconfig.OAuthServiceUrl);
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
            var appsettings = new Dictionary<string, string>()
            {
                ["security:oauth2:client:validateCertificates"] = "false",
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            // act
            var sconfig = new OAuthConnectorOptions(config);

            // assert
            Assert.False(sconfig.ValidateCertificates);
        }

        [Fact]
        public void Validate_Certificates_Binds()
        {
            // arrange a configuration with validateCertificates=false
            var appsettings = new Dictionary<string, string>()
            {
                ["security:oauth2:client:validate_certificates"] = "false",
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            // act
            var sconfig = new OAuthConnectorOptions(config);

            // assert
            Assert.False(sconfig.ValidateCertificates);
        }
    }
}
