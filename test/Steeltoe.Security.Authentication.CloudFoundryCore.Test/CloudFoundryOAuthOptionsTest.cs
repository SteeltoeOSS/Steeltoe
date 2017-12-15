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
