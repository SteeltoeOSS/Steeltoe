// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using System.Net;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class UriUtilityTest
    {
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
