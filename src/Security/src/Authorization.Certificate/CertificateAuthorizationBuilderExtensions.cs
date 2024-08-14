// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Certificates;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateAuthorizationBuilderExtensions
{
    /// <summary>
    /// Defines policies that verify the space/org in the incoming client certificate matches the space/org of the local application instance identity
    /// certificate in configuration.
    /// <para>
    /// Secure your endpoints with the included authorization policies by referencing <see cref="CertificateAuthorizationPolicies" />.
    /// </para>
    /// <para>
    /// This method also configures certificate forwarding.
    /// </para>
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthorizationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static AuthorizationBuilder AddOrgAndSpacePolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.ConfigureCertificateOptions(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);

        builder.Services.AddCertificateForwarding(_ =>
        {
        });

        builder.Services.AddSingleton<IPostConfigureOptions<CertificateAuthenticationOptions>, PostConfigureCertificateAuthenticationOptions>();
        builder.Services.AddSingleton<IAuthorizationHandler, CertificateAuthorizationHandler>();

        builder.AddPolicy(CertificateAuthorizationPolicies.SameOrganization, authorizationPolicyBuilder =>
        {
            authorizationPolicyBuilder.RequireSameOrg();
        }).AddPolicy(CertificateAuthorizationPolicies.SameSpace, authorizationPolicyBuilder =>
        {
            authorizationPolicyBuilder.RequireSameSpace();
        });

        return builder;
    }
}
