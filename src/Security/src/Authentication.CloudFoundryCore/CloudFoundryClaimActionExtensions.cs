// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using System;
using System.Security.Claims;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class CloudFoundryClaimActionExtensions
{
    public static void MapScopes(this ClaimActionCollection collection, string claimType = "scope")
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        collection.Add(new CloudFoundryScopeClaimAction(claimType, ClaimValueTypes.String));
    }
}