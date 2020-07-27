// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf.Test
{
    public class CloudFoundryOptionsConfigurerTest
    {
        [Fact]
        public void Configure_NoOptions_Throws()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => CloudFoundryOptionsConfigurer.Configure(null, null));
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void Configure_NoServiceInfo_ReturnsDefaults()
        {
            // arrange
            var opts = new CloudFoundryOptions();
            var authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

            // act
            CloudFoundryOptionsConfigurer.Configure(null, opts);

            // assert
            Assert.Equal(authURL, opts.AuthorizationUrl);
            Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
            Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateAudience);
            Assert.True(opts.ValidateCertificates);
            Assert.True(opts.ValidateIssuer);
            Assert.True(opts.ValidateLifetime);
        }

        [Fact]
        public void Configure_WithServiceInfo_ReturnsExpected()
        {
            // arrange
            var authURL = "http://domain";
            var opts = new CloudFoundryOptions();
            var info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");

            // act
            CloudFoundryOptionsConfigurer.Configure(info, opts);

            // assert
            Assert.Equal("clientId", opts.ClientId);
            Assert.Equal("secret", opts.ClientSecret);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }

        [Fact]
        public void Configure_AlwaysSetsTokenValidationParameters()
        {
            // arrange
            var opts = new CloudFoundryOptions() { ValidateAudience = false };

            // this property isn't set in the constructor or exposed downstream, it should be false here:
            Assert.Null(opts.TokenValidationParameters);

            // act
            CloudFoundryOptionsConfigurer.Configure(null, opts);

            // assert
            Assert.NotNull(opts.TokenValidationParameters);
            Assert.False(opts.TokenValidationParameters.ValidateAudience);
        }
    }
}
