// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.JwtBearer;

public static class JwtBearerServiceCollectionExtensions
{
    /// <summary>
    /// Configures <see cref="JwtBearerOptions" /> for compatibility with UAA-based systems, including those found in Cloud Foundry.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection ConfigureJwtBearerForCloudFoundry(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, PostConfigureJwtBearerOptions>();
        return services;
    }
}
