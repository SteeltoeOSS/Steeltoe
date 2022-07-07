// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class AuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder SameOrg(this AuthorizationPolicyBuilder builder)
    {
        builder.Requirements.Add(new SameOrgRequirement());
        return builder;
    }

    public static AuthorizationPolicyBuilder SameSpace(this AuthorizationPolicyBuilder builder)
    {
        builder.Requirements.Add(new SameSpaceRequirement());
        return builder;
    }
}
