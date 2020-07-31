// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP3_1
using Newtonsoft.Json.Linq;
#endif
using System.Security.Claims;
#if NETCOREAPP3_1
using System.Text.Json;
#endif
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryScopeClaimActionTest
    {
        [Fact]
        public void Run_AddsClaims()
        {
            var resp = TestHelpers.GetValidTokenInfoRequestResponse();
#if NETCOREAPP3_1
            var payload = JsonDocument.Parse(resp).RootElement;
#else
            var payload = JObject.Parse(resp);
#endif
            var action = new CloudFoundryScopeClaimAction("scope", ClaimValueTypes.String);
            var ident = new ClaimsIdentity();
            action.Run(payload, ident, "Issuer");
            Assert.NotEmpty(ident.Claims);
        }
    }
}
