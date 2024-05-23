// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Enable identity certificate rotation, certificate and header forwarding, authentication and authorization Default ForwardedHeadersOptions only
    /// includes <see cref="ForwardedHeaders.XForwardedProto" />.
    /// </summary>
    /// <param name="app">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <param name="forwardedHeaders">
    /// Custom header forwarding policy.
    /// </param>
    public static IApplicationBuilder UseCloudFoundryCertificateAuth(this IApplicationBuilder app, ForwardedHeadersOptions forwardedHeaders = null)
    {
        app.UseForwardedHeaders(forwardedHeaders ?? new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto
        });

        app.UseCertificateForwarding();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
