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
using System.Net;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class UriUtilityTest
    {
        [Fact]
        public void OriginalCallbackUri_Not_Changed()
        {
            // arrange
#pragma warning disable CS0618 // Type or member is obsolete
            var options = new OpenIDConnectOptions();
#pragma warning restore CS0618 // Type or member is obsolete
            var requestContext = OwinTestHelpers.CreateRequest("GET", string.Empty);

            // act
            var redirectUri = UriUtility.CalculateFullRedirectUri(options, requestContext.Request);

            // assert
            Assert.StartsWith(options.AuthDomain, redirectUri);
            Assert.Contains("response_type=code", redirectUri);
            Assert.Contains("scope=openid", redirectUri);
            Assert.EndsWith("redirect_uri=" + WebUtility.UrlEncode("http://localhost/signin-oidc"), redirectUri);
        }

        [Fact]
        public void Can_CalculateFullRedirectUri_WithDefaults()
        {
            // arrange
            var options = new OpenIdConnectOptions();
            var requestContext = OwinTestHelpers.CreateRequest("GET", string.Empty);

            // act
            var redirectUri = UriUtility.CalculateFullRedirectUri(options, requestContext.Request);

            // assert
            Assert.StartsWith(options.AuthDomain, redirectUri);
            Assert.Contains("response_type=code", redirectUri);
            Assert.Contains("scope=openid", redirectUri);
            Assert.EndsWith("redirect_uri=" + WebUtility.UrlEncode("http://localhost" + CloudFoundryDefaults.CallbackPath), redirectUri);
        }

        [Fact]
        public void Can_CalculateFullRedirectUri_WithNonDefaults()
        {
            // arrange
            var options = new OpenIdConnectOptions
            {
                AuthDomain = "my_oauth_server",
                CallbackPath = new PathString("/something_else")
            };
            var requestContext = OwinTestHelpers.CreateRequest("GET", string.Empty, "https", "some_server", 1234);

            // act
            var redirectUri = UriUtility.CalculateFullRedirectUri(options, requestContext.Request);

            // assert
            Assert.StartsWith(options.AuthDomain, redirectUri);
            Assert.Contains("response_type=code", redirectUri);
            Assert.Contains("scope=openid", redirectUri);
            Assert.EndsWith("redirect_uri=" + WebUtility.UrlEncode("https://some_server:1234/something_else"), redirectUri);
        }
    }
}
