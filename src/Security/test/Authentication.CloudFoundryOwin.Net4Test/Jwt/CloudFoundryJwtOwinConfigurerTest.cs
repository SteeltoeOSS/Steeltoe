// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Owin.Test
{
    public class CloudFoundryJwtOwinConfigurerTest
    {
        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            // arrange
            CloudFoundryJwtBearerAuthenticationOptions opts = new CloudFoundryJwtBearerAuthenticationOptions();

            // act
            CloudFoundryJwtOwinConfigurer.Configure(null, opts);

            // assert
            Assert.Equal("http://" + CloudFoundryDefaults.OAuthServiceUrl + CloudFoundryDefaults.JwtTokenUri, opts.JwtKeyUrl);
            Assert.True(opts.ValidateCertificates); // <- default value
            Assert.NotNull(opts.TokenValidationParameters);
        }

        [Fact]
        public void Configure_NoOptions_ReturnsExpected()
        {
            // arrange
            SsoServiceInfo info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");

            // act
            CloudFoundryJwtOwinConfigurer.Configure(info, null);

            // nothing to assert
            Assert.True(true, "If we got here, we didn't attempt to set properties on a null object");
        }

        [Fact]
        public void Configure_WithServiceInfo_ReturnsExpected()
        {
            // arrange
            CloudFoundryJwtBearerAuthenticationOptions opts = new CloudFoundryJwtBearerAuthenticationOptions();
            Assert.Null(opts.TokenValidationParameters);
            SsoServiceInfo info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");

            // act
            CloudFoundryJwtOwinConfigurer.Configure(info, opts);

            // assert
            Assert.Equal("http://domain" + CloudFoundryDefaults.JwtTokenUri, opts.JwtKeyUrl);
            Assert.True(opts.ValidateCertificates); // <- default value
            Assert.NotNull(opts.TokenValidationParameters);
        }
    }
}
