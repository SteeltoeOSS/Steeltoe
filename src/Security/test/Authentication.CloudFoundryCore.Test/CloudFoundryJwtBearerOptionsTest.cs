// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryJwtBearerOptionsTest
    {
        [Fact]
        public void DefaultConstructor_SetsupDefaultOptions()
        {
            var opts = new CloudFoundryJwtBearerOptions();

            var authURL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
            Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
            Assert.Equal(authURL + CloudFoundryDefaults.JwtTokenUri, opts.JwtKeyUrl);
            Assert.True(opts.SaveToken);
        }
    }
}
