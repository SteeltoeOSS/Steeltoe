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

using Microsoft.Owin;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class OpenIdConnectConfigurerTest
    {
        [Fact]
        public void Configure_NoOptions_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => OpenIdConnectConfigurer.Configure(null, null));
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsDefaults()
        {
            // arrange
            var opts = new OpenIdConnectOptions();
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

            // act
            OpenIdConnectConfigurer.Configure(null, opts);

            // assert
            Assert.Equal(CloudFoundryDefaults.DisplayName, opts.AuthenticationType);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), opts.CallbackPath);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }

        [Fact]
        public void Configure_ObsoleteVersion_NoServiceInfo_ReturnsDefaults()
        {
            // arrange
#pragma warning disable CS0618 // Type or member is obsolete
            var opts = new OpenIDConnectOptions();
#pragma warning restore CS0618 // Type or member is obsolete
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

            // act
            OpenIdConnectConfigurer.Configure(null, opts);

            // assert
            Assert.Equal("PivotalSSO", opts.AuthenticationType);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(new PathString("/signin-oidc"), opts.CallbackPath);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }

        [Fact]
        public void Configure_WithServiceInfo_ReturnsExpected()
        {
            // arrange
            string authURL = "http://domain";
            var opts = new OpenIdConnectOptions();
            SsoServiceInfo info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");

            // act
            OpenIdConnectConfigurer.Configure(info, opts);

            // assert
            Assert.Equal(CloudFoundryDefaults.DisplayName, opts.AuthenticationType);
            Assert.Equal("clientId", opts.ClientId);
            Assert.Equal("secret", opts.ClientSecret);
            Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), opts.CallbackPath);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }
    }
}
