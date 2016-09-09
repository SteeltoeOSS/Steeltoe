//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.AspNetCore.Http;
using SteelToe.CloudFoundry.Connector.OAuth;
using System.Runtime.InteropServices;

using Xunit;

namespace SteelToe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryOptionsTest
    {
        [Fact]
        public void DefaultConstructor_SetsupDefaultOptions()
        {
            CloudFoundryOptions opts = new CloudFoundryOptions();

            string authURL = "http://" + CloudFoundryOptions.Default_OAuthServiceUrl;
            Assert.Equal(CloudFoundryOptions.AUTHENTICATION_SCHEME, opts.ClaimsIssuer);
            Assert.Equal(CloudFoundryOptions.Default_ClientId, opts.ClientId );
            Assert.Equal(CloudFoundryOptions.Default_ClientSecret, opts.ClientSecret );
            Assert.Equal(CloudFoundryOptions.OAUTH_AUTHENTICATION_SCHEME, opts.AuthenticationScheme );
            Assert.Equal(CloudFoundryOptions.AUTHENTICATION_SCHEME, opts.DisplayName );
            Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath );
            Assert.Equal(authURL + CloudFoundryOptions.Default_AuthorizationUri, opts.AuthorizationEndpoint );
            Assert.Equal(authURL + CloudFoundryOptions.Default_AccessTokenUri, opts.TokenEndpoint ) ;
            Assert.Equal(authURL + CloudFoundryOptions.Default_UserInfoUri, opts.UserInformationEndpoint );
            Assert.Equal(authURL + CloudFoundryOptions.Default_CheckTokenUri, opts.TokenInfoUrl) ;
            Assert.Equal(authURL + CloudFoundryOptions.Default_JwtTokenKey, opts.JwtKeyUrl );
            Assert.True(opts.ValidateCertificates);

        }
        [Fact]
        public void OAuthServiceOptionsConstructor_SetsupOptionsAsExpected()
        {
            OAuthServiceOptions oauthOpts = new OAuthServiceOptions()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret",
                UserAuthorizationUrl = "UserAuthorizationUrl",
                AccessTokenUrl = "AccessTokenUrl",
                UserInfoUrl = "UserInfoUrl",
                TokenInfoUrl = "TokenInfoUrl",
                JwtKeyUrl = "JwtKeyUrl",
                Scope = { "foo", "bar" }
            };

            CloudFoundryOptions opts = new CloudFoundryOptions(oauthOpts);

            Assert.Equal(CloudFoundryOptions.AUTHENTICATION_SCHEME, opts.ClaimsIssuer);
            Assert.Equal("ClientId", opts.ClientId);
            Assert.Equal("ClientSecret", opts.ClientSecret);
            Assert.Equal(CloudFoundryOptions.OAUTH_AUTHENTICATION_SCHEME, opts.AuthenticationScheme);
            Assert.Equal(CloudFoundryOptions.AUTHENTICATION_SCHEME, opts.DisplayName);
            Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
            Assert.Equal("UserAuthorizationUrl", opts.AuthorizationEndpoint);
            Assert.Equal("AccessTokenUrl", opts.TokenEndpoint);
            Assert.Equal("UserInfoUrl", opts.UserInformationEndpoint);
            Assert.Equal("TokenInfoUrl", opts.TokenInfoUrl);
            Assert.Equal("JwtKeyUrl", opts.JwtKeyUrl);
            Assert.True(opts.Scope.Contains("foo"));
            Assert.True(opts.Scope.Contains("bar"));
            Assert.True(opts.ValidateCertificates);

        }

        [Fact]
        public void GetBackChannelHandler_ReturnsCorrectly()
        {


            CloudFoundryOptions opts = new CloudFoundryOptions();
            Assert.Null(opts.GetBackChannelHandler());

            opts = new CloudFoundryOptions()
            {
                ValidateCertificates = false
            };
#if NET451
            Assert.Null(opts.GetBackChannelHandler());

#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.NotNull(opts.GetBackChannelHandler());
#endif

            OAuthServiceOptions oauthOpts = new OAuthServiceOptions()
            {
                ClientId = "ClientId",
                ClientSecret = "ClientSecret",
                UserAuthorizationUrl = "UserAuthorizationUrl",
                AccessTokenUrl = "AccessTokenUrl",
                UserInfoUrl = "UserInfoUrl",
                TokenInfoUrl = "TokenInfoUrl",
                JwtKeyUrl = "JwtKeyUrl",
                Scope = { "foo", "bar" }
            };

            opts = new CloudFoundryOptions(oauthOpts);
            Assert.Null(opts.GetBackChannelHandler());

            opts = new CloudFoundryOptions(oauthOpts)
            {
                ValidateCertificates = false
            };

#if NET451
            Assert.Null(opts.GetBackChannelHandler());

#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Assert.NotNull(opts.GetBackChannelHandler());
#endif

        }
    }
}
