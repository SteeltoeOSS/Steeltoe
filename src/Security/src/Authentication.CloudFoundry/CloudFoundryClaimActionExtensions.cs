// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class CloudFoundryClaimActionExtensions
{
    public static void MapScopes(this ClaimActionCollection collection, string claimType = "scope")
    {
        ArgumentGuard.NotNull(collection);

        collection.Add(new CloudFoundryScopeClaimAction(claimType, ClaimValueTypes.String));
    }
}
