// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateAuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// Require a client certificate to originate from within the same organization.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthorizationPolicyBuilder" />.
    /// </param>
    public static AuthorizationPolicyBuilder RequireSameOrg(this AuthorizationPolicyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Requirements.Add(new SameOrgRequirement());
        return builder;
    }

    /// <summary>
    /// Require a client certificate to originate from within the same space.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthorizationPolicyBuilder" />.
    /// </param>
    public static AuthorizationPolicyBuilder RequireSameSpace(this AuthorizationPolicyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Requirements.Add(new SameSpaceRequirement());
        return builder;
    }
}
