// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.OpenIdConnect;

public static class OpenIdConnectServiceCollectionExtensions
{
    /// <summary>
    /// Configure <see cref="OpenIdConnectOptions" /> for compatibility with UAA-based systems, including Single Sign-On for VMware Tanzu Application
    /// Service.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection ConfigureOpenIdConnectForCloudFoundry(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, PostConfigureOpenIdConnectOptions>();
        return services;
    }
}
