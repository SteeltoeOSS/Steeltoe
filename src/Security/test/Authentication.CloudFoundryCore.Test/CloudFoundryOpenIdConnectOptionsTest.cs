// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryOpenIdConnectOptionsTest
    {
        private const string DEFAULT_OAUTH_SERVICE_URL = "https://" + CloudFoundryDefaults.OAuthServiceUrl;

        [Fact]
        public void DefaultConstructor_SetsDefaultOptions()
        {
            var opts = new CloudFoundryOpenIdConnectOptions();

            Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
            Assert.Equal(DEFAULT_OAUTH_SERVICE_URL, opts.Authority);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
            Assert.True(opts.ValidateCertificates);
#if NETCOREAPP3_1 || NET5_0
            Assert.Equal(19, opts.ClaimActions.Count());
#else
            Assert.Equal(21, opts.ClaimActions.Count());
#endif
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
            Assert.False(opts.SaveTokens);
        }

        [Theory]
        [MemberData(nameof(SetEndpointsData))]
        public void SetEndpoints_WithNewDomain_ReturnsExpected(string newDomain, string expectedUrl)
        {
            var options = new CloudFoundryOpenIdConnectOptions();

            options.SetEndpoints(newDomain);

            Assert.Equal(expectedUrl, options.Authority);
        }

        public static TheoryData<string, string> SetEndpointsData()
        {
            var data = new TheoryData<string, string>();
            var newDomain = "http://not-the-original-domain";

            data.Add(newDomain, newDomain);
            data.Add(string.Empty, DEFAULT_OAUTH_SERVICE_URL);
            data.Add("   ", DEFAULT_OAUTH_SERVICE_URL);
            data.Add(default, DEFAULT_OAUTH_SERVICE_URL);

            return data;
        }
    }
}
