// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateApplicationBuilderExtensions
{
    /// <summary>
    /// Enables certificate and header forwarding, along with ASP.NET Core authentication and authorization middlewares. Sets ForwardedHeaders to
    /// <see cref="ForwardedHeaders.XForwardedProto" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UseCertificateAuthorization(this IApplicationBuilder builder)
    {
        return UseCertificateAuthorization(builder, new ForwardedHeadersOptions());
    }

    /// <summary>
    /// Enables certificate and header forwarding, along with ASP.NET Core authentication and authorization middlewares.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="options">
    /// Custom header forwarding policy. <see cref="ForwardedHeaders.XForwardedProto" /> is added to your <see cref="ForwardedHeadersOptions" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UseCertificateAuthorization(this IApplicationBuilder builder, ForwardedHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        options.ForwardedHeaders |= ForwardedHeaders.XForwardedProto;

        builder.UseForwardedHeaders(options);
        builder.UseCertificateForwarding();
        builder.UseAuthentication();
        builder.UseAuthorization();

        return builder;
    }
}
