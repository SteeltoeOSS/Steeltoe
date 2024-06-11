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
    /// Enable certificate and header forwarding, along with ASP.NET Core authentication and authorization middlewares. Sets ForwardedHeaders to
    /// <see cref="ForwardedHeaders.XForwardedProto" />.
    /// </summary>
    /// <param name="app">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    public static IApplicationBuilder UseCertificateAuthorization(this IApplicationBuilder app)
    {
        return app.UseCertificateAuthorization(new ForwardedHeadersOptions());
    }

    /// <summary>
    /// Enable certificate and header forwarding, along with ASP.NET Core authentication and authorization middlewares.
    /// </summary>
    /// <param name="app">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <param name="forwardedHeaders">
    /// Custom header forwarding policy. <see cref="ForwardedHeaders.XForwardedProto" /> is added to your <see cref="ForwardedHeadersOptions" />.
    /// </param>
    public static IApplicationBuilder UseCertificateAuthorization(this IApplicationBuilder app, ForwardedHeadersOptions forwardedHeaders)
    {
        ArgumentGuard.NotNull(forwardedHeaders);

        forwardedHeaders.ForwardedHeaders |= ForwardedHeaders.XForwardedProto;

        app.UseForwardedHeaders(forwardedHeaders);
        app.UseCertificateForwarding();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
