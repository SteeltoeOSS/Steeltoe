// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Certificates;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateAuthorizationBuilderExtensions
{
    /// <summary>
    /// Adds the necessary components and policies for server-side authorization of application instance identity certificates.
    /// <para>
    /// Components include <see cref="CertificateOptions" /> named "AppInstanceIdentity" and certificate forwarding.
    /// </para>
    /// <para>
    /// Secure your endpoints with the included authorization policies by referencing <see cref="CertificateAuthorizationPolicies" />.
    /// </para>
    /// </summary>
    /// <param name="authorizationBuilder">
    /// The <see cref="AuthorizationBuilder" />.
    /// </param>
    public static AuthorizationBuilder AddAppInstanceIdentityCertificate(this AuthorizationBuilder authorizationBuilder)
    {
        ArgumentGuard.NotNull(authorizationBuilder);

        authorizationBuilder.Services.ConfigureCertificateOptions(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);

        authorizationBuilder.Services.AddCertificateForwarding(_ =>
        {
        });

        authorizationBuilder.Services.AddSingleton<IPostConfigureOptions<CertificateAuthenticationOptions>, PostConfigureCertificateAuthenticationOptions>();
        authorizationBuilder.Services.AddSingleton<IAuthorizationHandler, CertificateAuthorizationHandler>();

        authorizationBuilder.AddPolicy(CertificateAuthorizationPolicies.SameOrganization, authorizationPolicyBuilder =>
        {
            authorizationPolicyBuilder.RequireSameOrg();
        }).AddPolicy(CertificateAuthorizationPolicies.SameSpace, authorizationPolicyBuilder =>
        {
            authorizationPolicyBuilder.RequireSameSpace();
        });

        return authorizationBuilder;
    }
}
