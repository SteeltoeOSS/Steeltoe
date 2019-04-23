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

using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf.Test
{
    public class CloudFoundryOptionsTest
    {
        [Fact]
        public void ParameterlessConstructor_StillReadsEnvVars()
        {
            // arrange
            Environment.SetEnvironmentVariable("sso_auth_domain", "auth_domain");
            Environment.SetEnvironmentVariable("sso_client_id", "ssoClientId");
            Environment.SetEnvironmentVariable("sso_client_secret", "ssoClientSecret");

            // act
            var options = new CloudFoundryOptions();

            // assert
            Assert.Equal("auth_domain", options.AuthorizationUrl);
            Assert.Equal("ssoClientId", options.ClientId);
            Assert.Equal("ssoClientSecret", options.ClientSecret);

            // reset the env
            Environment.SetEnvironmentVariable("sso_auth_domain", string.Empty);
            Environment.SetEnvironmentVariable("sso_client_id", string.Empty);
            Environment.SetEnvironmentVariable("sso_client_secret", string.Empty);
        }
    }
}
