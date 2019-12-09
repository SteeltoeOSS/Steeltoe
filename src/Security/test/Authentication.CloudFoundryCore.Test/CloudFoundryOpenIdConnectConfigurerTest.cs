﻿// Copyright 2017 the original author or authors.
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
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Steeltoe.Connector.Services;
using System.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryOpenIdConnectConfigurerTest
    {
        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            // arrange
            var oidcOptions = new OpenIdConnectOptions();

            // act
            CloudFoundryOpenIdConnectConfigurer.Configure(null, oidcOptions, new CloudFoundryOpenIdConnectOptions() { ValidateCertificates = false });

            // assert
            Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, oidcOptions.ClaimsIssuer);
            Assert.Equal(CloudFoundryDefaults.ClientId, oidcOptions.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, oidcOptions.ClientSecret);
            Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), oidcOptions.CallbackPath);
#if NETCOREAPP3_0
            Assert.Equal(19, oidcOptions.ClaimActions.Count());
#else
            Assert.Equal(21, oidcOptions.ClaimActions.Count());
#endif
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, oidcOptions.SignInScheme);
            Assert.False(oidcOptions.SaveTokens);
            Assert.NotNull(oidcOptions.BackchannelHttpHandler);
        }

        [Fact]
        public void Configure_WithServiceInfo_ReturnsExpected()
        {
            // arrange
            var authURL = "https://domain";
            var oidcOptions = new OpenIdConnectOptions();
            var info = new SsoServiceInfo("foobar", "clientId", "secret", authURL);

            // act
            CloudFoundryOpenIdConnectConfigurer.Configure(info, oidcOptions, new CloudFoundryOpenIdConnectOptions());

            // assert
            Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, oidcOptions.ClaimsIssuer);
            Assert.Equal(authURL, oidcOptions.Authority);
            Assert.Equal("clientId", oidcOptions.ClientId);
            Assert.Equal("secret", oidcOptions.ClientSecret);
            Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), oidcOptions.CallbackPath);
            Assert.Null(oidcOptions.BackchannelHttpHandler);
#if NETCOREAPP3_0
            Assert.Equal(19, oidcOptions.ClaimActions.Count());
#else
            Assert.Equal(21, oidcOptions.ClaimActions.Count());
#endif
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, oidcOptions.SignInScheme);
            Assert.False(oidcOptions.SaveTokens);
            Assert.Null(oidcOptions.BackchannelHttpHandler);
        }
    }
}
