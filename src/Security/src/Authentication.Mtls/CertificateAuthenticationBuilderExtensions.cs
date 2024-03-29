// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;

namespace Steeltoe.Security.Authentication.Mtls;

public static class CertificateAuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds certificate authentication.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthenticationBuilder" />.
    /// </param>
    public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder)
    {
        return builder.AddMutualTls(CertificateAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Adds certificate authentication.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// Scheme identifier.
    /// </param>
    public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder, string authenticationScheme)
    {
        return builder.AddMutualTls(authenticationScheme, null);
    }

    /// <summary>
    /// Adds certificate authentication.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="configureOptions">
    /// Additional options configuration.
    /// </param>
    public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder, Action<MutualTlsAuthenticationOptions> configureOptions)
    {
        return builder.AddMutualTls(CertificateAuthenticationDefaults.AuthenticationScheme, configureOptions);
    }

    /// <summary>
    /// Adds certificate authentication.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthenticationBuilder" />.
    /// </param>
    /// <param name="authenticationScheme">
    /// Scheme identifier.
    /// </param>
    /// <param name="configureOptions">
    /// Additional options configuration.
    /// </param>
    public static AuthenticationBuilder AddMutualTls(this AuthenticationBuilder builder, string authenticationScheme,
        Action<MutualTlsAuthenticationOptions> configureOptions)
    {
        return builder.AddScheme<MutualTlsAuthenticationOptions, MutualTlsAuthenticationHandler>(authenticationScheme, configureOptions);
    }
}
