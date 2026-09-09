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
    [Obsolete("Trusting a certificate header without verifying that a reverse proxy set it after a successful mTLS handshake may enable identity spoofing. " +
        $"This overload will be removed in a future release. Use {nameof(AddOrgAndSpacePoliciesForMutualTls)} instead.")]
    public static AuthorizationBuilder AddOrgAndSpacePolicies(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return Configure(builder, null);
    }

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
    /// <param name="certificateHeaderName">
    /// The name of the HTTP header used to pass the certificate.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    [Obsolete("Configuring a custom header name is no longer recommended because it may enable identity spoofing. " +
        $"This overload will be removed in a future release. Use {nameof(AddOrgAndSpacePoliciesForMutualTls)} instead.")]
    public static AuthorizationBuilder AddOrgAndSpacePolicies(this AuthorizationBuilder builder, string? certificateHeaderName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return Configure(builder, certificateHeaderName);
    }

    /// <summary>
    /// Defines policies that verify the space/org in the incoming client certificate matches the space/org of the local application instance identity
    /// certificate in configuration.
    /// <para>
    /// Secure your endpoints with the included authorization policies by referencing <see cref="CertificateAuthorizationPolicies" />.
    /// </para>
    /// <para>
    /// This method also configures certificate forwarding, trusting only the
    /// <c>
    /// X-Forwarded-Client-Cert
    /// </c>
    /// header.
    /// </para>
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthorizationBuilder" /> to configure.
    /// </param>
    /// <remarks>
    /// <para>
    /// This requires the Cloud Foundry Gorouter to be configured as the point of TLS termination.
    /// </para>
    /// <para>
    /// When configured correctly, Gorouter terminates the mutual TLS handshake and forwards the verified client certificate in the
    /// <c>
    /// X-Forwarded-Client-Cert
    /// </c>
    /// header. Gorouter also strips any incoming instances of this header from external clients, protecting applications from spoofing.
    /// </para>
    /// <para>
    /// If TLS termination is not enabled at the router, this header is never set and the configured policies will reject every request.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static AuthorizationBuilder AddOrgAndSpacePoliciesForMutualTls(this AuthorizationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return Configure(builder, "X-Forwarded-Client-Cert");
    }

    private static AuthorizationBuilder Configure(AuthorizationBuilder builder, string? certificateHeaderName)
    {
        builder.Services.ConfigureCertificateOptions(CertificateConfigurationExtensions.AppInstanceIdentityCertificateName);

        builder.Services.AddCertificateForwarding(options =>
        {
            if (certificateHeaderName != null)
            {
                options.CertificateHeader = certificateHeaderName;
            }
        });

        builder.Services.AddSingleton<IPostConfigureOptions<CertificateAuthenticationOptions>, PostConfigureCertificateAuthenticationOptions>();
        builder.Services.AddSingleton<IAuthorizationHandler, CertificateAuthorizationHandler>();

        builder.AddPolicy(CertificateAuthorizationPolicies.SameOrg, authorizationPolicyBuilder =>
        {
            authorizationPolicyBuilder.RequireSameOrg();
        }).AddPolicy(CertificateAuthorizationPolicies.SameSpace, authorizationPolicyBuilder =>
        {
            authorizationPolicyBuilder.RequireSameSpace();
        });

        return builder;
    }
}
