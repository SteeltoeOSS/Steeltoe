// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class OpenIdConnectConfigurerTest
    {
        [Fact]
        public void Configure_NoOptions_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => OpenIdConnectConfigurer.Configure(null, null));
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsDefaults()
        {
            // arrange
            var opts = new OpenIdConnectOptions();
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

            // act
            OpenIdConnectConfigurer.Configure(null, opts);

            // assert
            Assert.Equal(CloudFoundryDefaults.DisplayName, opts.AuthenticationType);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), opts.CallbackPath);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }

        [Fact]
        public void Configure_WithServiceInfo_ReturnsExpected()
        {
            // arrange
            string authURL = "http://domain";
            var opts = new OpenIdConnectOptions();
            SsoServiceInfo info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");

            // act
            OpenIdConnectConfigurer.Configure(info, opts);

            // assert
            Assert.Equal(CloudFoundryDefaults.DisplayName, opts.AuthenticationType);
            Assert.Equal("clientId", opts.ClientId);
            Assert.Equal("secret", opts.ClientSecret);
            Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), opts.CallbackPath);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }
    }
}
