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

using Microsoft.Owin.Security;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class OpenIdConnectOptionsTest
    {
        [Fact]
        public void Options_Use_CloudFoundryDefaults()
        {
            // act
            var options = new OpenIdConnectOptions();

            // assert
            Assert.Equal(CloudFoundryDefaults.DisplayName, options.AuthenticationType);
            Assert.Equal(CloudFoundryDefaults.ClientId, options.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, options.ClientSecret);
            Assert.Equal("http://" + CloudFoundryDefaults.OAuthServiceUrl, options.AuthDomain);
            Assert.Equal("http://" + CloudFoundryDefaults.OAuthServiceUrl + CloudFoundryDefaults.CheckTokenUri, options.TokenInfoUrl);
            Assert.Equal(CloudFoundryDefaults.ClientId, options.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientId, options.ClientId);
        }

        [Fact]
        public void Options_Can_Change_AuthenticationType()
        {
            // arrange
            var authType = "NotTheDefault";

            // act
            var options = new OpenIdConnectOptions(authType);

            // assert
            Assert.Equal(authType, options.AuthenticationType);
            Assert.Equal(authType, options.Description.Caption);
            Assert.Equal(AuthenticationMode.Passive, options.AuthenticationMode);
        }

        [Fact]
        public void OpenIdOptions_CanProduce_AuthServerOptions()
        {
            // arrange
            var options = new OpenIdConnectOptions
            {
                ClientId = "notDefault",
                ClientSecret = "notDefault",
                ValidateCertificates = false,
                AdditionalScopes = "banana apple orange"
            };

            // act
            var produce = options.AsAuthServerOptions();

            // assert
            Assert.Equal(produce.AuthorizationUrl, options.AuthDomain + CloudFoundryDefaults.AccessTokenUri);
            Assert.Equal(produce.ClientId, options.ClientId);
            Assert.Equal(produce.ClientSecret, options.ClientSecret);
            Assert.Equal(produce.ValidateCertificates, options.ValidateCertificates);
            Assert.Equal(produce.AdditionalTokenScopes, options.AdditionalScopes);
        }
    }
}
