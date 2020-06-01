// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
