// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Steeltoe.Common;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateAuthorizationPolicyBuilderExtensions
{
    public static AuthorizationPolicyBuilder RequireSameOrg(this AuthorizationPolicyBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        builder.Requirements.Add(new SameOrgRequirement());
        return builder;
    }

    public static AuthorizationPolicyBuilder RequireSameSpace(this AuthorizationPolicyBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        builder.Requirements.Add(new SameSpaceRequirement());
        return builder;
    }
}
