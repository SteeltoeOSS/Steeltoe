// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Steeltoe.Security.Authentication.OpenIdConnect;

public static class OpenIdConnectAuthenticationBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="OpenIdConnectOptions" /> for compatibility with UAA-based systems, including those found in Cloud Foundry.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="AuthenticationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static AuthenticationBuilder ConfigureOpenIdConnectForCloudFoundry(this AuthenticationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, PostConfigureOpenIdConnectOptions>();
        return builder;
    }
}
