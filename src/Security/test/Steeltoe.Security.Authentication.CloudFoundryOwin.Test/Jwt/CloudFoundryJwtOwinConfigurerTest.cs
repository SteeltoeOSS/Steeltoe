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
