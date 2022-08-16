// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryScopeClaimAction : ClaimAction
{
    public CloudFoundryScopeClaimAction(string claimType, string valueType)
        : base(claimType, valueType)
    {
    }

    public override void Run(JsonElement userData, ClaimsIdentity identity, string issuer)
    {
        ICollection<string> scopes = CloudFoundryHelper.GetScopes(userData);

        if (scopes != null)
        {
            foreach (string s in scopes)
            {
                identity.AddClaim(new Claim(ClaimType, s, ValueType, issuer));
            }
        }
    }
}
