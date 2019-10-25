﻿// Copyright 2017 the original author or authors.
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

#if !NETCOREAPP3_0
using Newtonsoft.Json.Linq;
#endif
using System;
#if NETCOREAPP3_0
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
#if NETCOREAPP3_0
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
#if NETCOREAPP3_0
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
#if NETCOREAPP3_0
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
