// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
