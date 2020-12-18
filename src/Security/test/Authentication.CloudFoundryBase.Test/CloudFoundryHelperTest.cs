// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP3_1 && !NET5_0
using Newtonsoft.Json.Linq;
#endif
using System;
#if NETCOREAPP3_1 || NET5_0
using System.Text.Json;
#endif
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryHelperTest
    {
        [Fact]
        public void GetBackChannelHandler_ReturnsExpected()
        {
            var result1 = CloudFoundryHelper.GetBackChannelHandler(false);
            Assert.NotNull(result1);

            var result2 = CloudFoundryHelper.GetBackChannelHandler(true);
            Assert.Null(result2);
        }

        [Fact]
        public void GetTokenValidationParameters_ReturnsExpected()
        {
            var parameters = CloudFoundryHelper.GetTokenValidationParameters(null, "https://foo.bar.com/keyurl", null, false);
            Assert.False(parameters.ValidateAudience, "Audience validation should not be enabled by default");
            Assert.True(parameters.ValidateIssuer, "Issuer validation should be enabled by default");
            Assert.NotNull(parameters.IssuerValidator);
            Assert.True(parameters.ValidateLifetime, "Token lifetime validation should be enabled by default");
            Assert.NotNull(parameters.IssuerSigningKeyResolver);
        }

        [Fact]
        public void GetExpTime_FindsTime()
        {
            var info = TestHelpers.GetValidTokenInfoRequestResponse();
#if NETCOREAPP3_1 || NET5_0
            var payload = JsonDocument.Parse(info).RootElement;
#else
            var payload = JObject.Parse(info);
#endif
            var dateTime = CloudFoundryHelper.GetExpTime(payload);
            Assert.Equal(new DateTime(2016, 9, 2, 8, 04, 23, DateTimeKind.Utc), dateTime);
        }

        [Fact]
        public void GetIssueTime_FindsTime()
        {
            var info = TestHelpers.GetValidTokenInfoRequestResponse();
#if NETCOREAPP3_1 || NET5_0
            var payload = JsonDocument.Parse(info).RootElement;
#else
            var payload = JObject.Parse(info);
#endif
            var dateTime = CloudFoundryHelper.GetIssueTime(payload);
            Assert.Equal(new DateTime(2016, 9, 1, 20, 04, 23, DateTimeKind.Utc), dateTime);
        }

        [Fact]
        public void GetScopes_FindsScopes()
        {
            var info = TestHelpers.GetValidTokenInfoRequestResponse();
#if NETCOREAPP3_1 || NET5_0
            var payload = JsonDocument.Parse(info).RootElement;
#else
            var payload = JObject.Parse(info);
#endif
            var scopes = CloudFoundryHelper.GetScopes(payload);
            Assert.Contains("openid", scopes);
            Assert.Single(scopes);
        }
    }
}
