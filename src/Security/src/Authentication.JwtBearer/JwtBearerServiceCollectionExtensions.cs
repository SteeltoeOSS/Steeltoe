// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Security.Authentication.Shared;

namespace Steeltoe.Security.Authentication.JwtBearer;

public static class JwtBearerServiceCollectionExtensions
{
    /// <summary>
    /// Configure <see cref="JwtBearerOptions" /> for compatibility with UAA-based systems, including Single Sign-On for VMware Tanzu Platform.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    public static IServiceCollection ConfigureJwtBearerForCloudFoundry(this IServiceCollection services)
    {
        return ConfigureJwtBearerForCloudFoundry(services, null);
    }

    /// <summary>
    /// Configure <see cref="JwtBearerOptions" /> for compatibility with UAA-based systems, including Single Sign-On for VMware Tanzu Platform.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureHttpClient">
    /// Configures the <see cref="HttpClient"/> used to interact with the identity server.
    /// </param>
    public static IServiceCollection ConfigureJwtBearerForCloudFoundry(this IServiceCollection services, Action<HttpClient>? configureHttpClient)
    {
        ArgumentGuard.NotNull(services);

        if (services.Any(descriptor => descriptor.ServiceType.IsAssignableFrom(typeof(IPostConfigureOptions<JwtBearerOptions>))))
        {
            throw new InvalidOperationException(
                $"{nameof(ConfigureJwtBearerForCloudFoundry)} must be called before {nameof(JwtBearerExtensions.AddJwtBearer)}.");
        }

        services.AddSteeltoeSecurityHttpClient(configureHttpClient);
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, PostConfigureJwtBearerOptions>();
        return services;
    }
}
