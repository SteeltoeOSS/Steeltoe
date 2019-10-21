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

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryOpenIdConnectOptionsTest
    {
        [Fact]
        public void DefaultConstructor_SetsDefaultOptions()
        {
            var opts = new CloudFoundryOpenIdConnectOptions();

            Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
            Assert.Equal("https://" + CloudFoundryDefaults.OAuthServiceUrl, opts.Authority);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
            Assert.True(opts.ValidateCertificates);
#if NETCOREAPP3_0
            Assert.Equal(19, opts.ClaimActions.Count());
#else
            Assert.Equal(21, opts.ClaimActions.Count());
#endif
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
            Assert.False(opts.SaveTokens);
        }
    }
}
