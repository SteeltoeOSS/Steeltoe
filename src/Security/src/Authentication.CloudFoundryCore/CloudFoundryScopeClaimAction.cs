// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.OAuth.Claims;
#if NETSTANDARD2_0
using Newtonsoft.Json.Linq;
#endif
using System.Security.Claims;
#if NETCOREAPP3_0
using System.Text.Json;
#endif

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryScopeClaimAction : ClaimAction
    {
        public CloudFoundryScopeClaimAction(string claimType, string valueType)
            : base(claimType, valueType)
        {
        }

#if NETCOREAPP3_0
        public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
#else
        public override void Run(JObject userData, ClaimsIdentity identity, string issuer)
#endif
        {
            var scopes = CloudFoundryHelper.GetScopes(userData);
            if (scopes != null)
            {
                foreach (var s in scopes)
                {
                    identity.AddClaim(new Claim(ClaimType, s, ValueType, issuer));
                }
            }
        }
    }
}
