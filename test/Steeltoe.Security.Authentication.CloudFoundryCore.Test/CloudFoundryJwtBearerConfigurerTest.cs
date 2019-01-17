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
