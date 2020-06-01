// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Steeltoe.CloudFoundry.Connector.Services;

using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryJwtBearerConfigurerTest
    {
        [Fact]
        public void Configure_NoServiceInfo_ReturnsExpected()
        {
            CloudFoundryJwtBearerOptions opts = new CloudFoundryJwtBearerOptions();
            JwtBearerOptions jwtOpts = new JwtBearerOptions();

            CloudFoundryJwtBearerConfigurer.Configure(null, jwtOpts, opts);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(opts.ClaimsIssuer, jwtOpts.ClaimsIssuer);
            Assert.Null(jwtOpts.BackchannelHttpHandler);
            Assert.NotNull(jwtOpts.TokenValidationParameters);
            Assert.Equal(opts.SaveToken, jwtOpts.SaveToken);
        }

        [Fact]
        public void Configure_WithServiceInfo_ReturnsExpected()
        {
            CloudFoundryJwtBearerOptions opts = new CloudFoundryJwtBearerOptions();
            SsoServiceInfo info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");
            JwtBearerOptions jwtOpts = new JwtBearerOptions();

            CloudFoundryJwtBearerConfigurer.Configure(info, jwtOpts, opts);
            Assert.Equal("http://domain" + CloudFoundryDefaults.JwtTokenUri, opts.JwtKeyUrl);
            Assert.True(opts.ValidateCertificates);
            Assert.Equal(opts.ClaimsIssuer, jwtOpts.ClaimsIssuer);
            Assert.Null(jwtOpts.BackchannelHttpHandler);
            Assert.NotNull(jwtOpts.TokenValidationParameters);
            Assert.Equal(opts.SaveToken, jwtOpts.SaveToken);
        }
    }
}
