// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Linq;

using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryOAuthOptionsTest
    {
        [Fact]
        public void DefaultConstructor_SetsupDefaultOptions()
        {
            CloudFoundryOAuthOptions opts = new CloudFoundryOAuthOptions();

            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
            Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
            Assert.Equal(authURL + CloudFoundryDefaults.AuthorizationUri, opts.AuthorizationEndpoint);
            Assert.Equal(authURL + CloudFoundryDefaults.AccessTokenUri, opts.TokenEndpoint);
            Assert.Equal(authURL + CloudFoundryDefaults.UserInfoUri, opts.UserInformationEndpoint);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(6, opts.ClaimActions.Count());
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
            Assert.True(opts.SaveTokens);
        }
    }
}
