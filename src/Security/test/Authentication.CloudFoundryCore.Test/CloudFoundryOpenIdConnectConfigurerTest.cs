// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Steeltoe.CloudFoundry.Connector.Services;
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
            Assert.Equal(19, oidcOptions.ClaimActions.Count());
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
            Assert.Equal(19, oidcOptions.ClaimActions.Count());
            Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, oidcOptions.SignInScheme);
            Assert.False(oidcOptions.SaveTokens);
            Assert.Null(oidcOptions.BackchannelHttpHandler);
        }
    }
}
