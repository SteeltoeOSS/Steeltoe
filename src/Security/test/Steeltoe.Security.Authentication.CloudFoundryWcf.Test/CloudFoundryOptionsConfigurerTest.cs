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
            string authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;

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
            string authURL = "http://domain";
            var opts = new CloudFoundryOptions();
            SsoServiceInfo info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");

            // act
            CloudFoundryOptionsConfigurer.Configure(info, opts);

            // assert
            Assert.Equal("clientId", opts.ClientId);
            Assert.Equal("secret", opts.ClientSecret);
            Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
            Assert.True(opts.ValidateCertificates);
        }
    }
}
