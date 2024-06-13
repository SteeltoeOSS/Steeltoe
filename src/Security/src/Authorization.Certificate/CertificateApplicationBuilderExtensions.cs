// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Steeltoe.Common;

namespace Steeltoe.Security.Authorization.Certificate;

public static class CertificateApplicationBuilderExtensions
{
    /// <summary>
    /// Enables certificate and header forwarding, along with ASP.NET Core authentication and authorization middlewares. Sets ForwardedHeaders to
    /// <see cref="ForwardedHeaders.XForwardedProto" />.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    public static IApplicationBuilder UseCertificateAuthorization(this IApplicationBuilder applicationBuilder)
    {
        return UseCertificateAuthorization(applicationBuilder, new ForwardedHeadersOptions());
    }

    /// <summary>
    /// Enables certificate and header forwarding, along with ASP.NET Core authentication and authorization middlewares.
    /// </summary>
    /// <param name="applicationBuilder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <param name="options">
    /// Custom header forwarding policy. <see cref="ForwardedHeaders.XForwardedProto" /> is added to your <see cref="ForwardedHeadersOptions" />.
    /// </param>
    public static IApplicationBuilder UseCertificateAuthorization(this IApplicationBuilder applicationBuilder, ForwardedHeadersOptions options)
    {
        ArgumentGuard.NotNull(applicationBuilder);
        ArgumentGuard.NotNull(options);

        options.ForwardedHeaders |= ForwardedHeaders.XForwardedProto;

        applicationBuilder.UseForwardedHeaders(options);
        applicationBuilder.UseCertificateForwarding();
        applicationBuilder.UseAuthentication();
        applicationBuilder.UseAuthorization();

        return applicationBuilder;
    }
}
