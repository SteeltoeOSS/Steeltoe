// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.OAuth;

public static class OAuthServiceCollectionExtensions
{
    /// <summary>
    /// Adds OAuthServiceOptions to Service Collection.
    /// </summary>
    /// <param name="services">
    /// Your Service Collection.
    /// </param>
    /// <param name="config">
    /// Application Configuration.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddOAuthServiceOptions(this IServiceCollection services, IConfiguration config)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(config);

        var oauthConfig = new OAuthConnectorOptions(config);
        var info = config.GetSingletonServiceInfo<SsoServiceInfo>();
        var factory = new OAuthConnectorFactory(info, oauthConfig);
        services.AddSingleton(typeof(IOptions<OAuthServiceOptions>), factory.Create);
        return services;
    }

    /// <summary>
    /// Adds OAuthServiceOptions to Service Collection.
    /// </summary>
    /// <param name="services">
    /// Your Service Collection.
    /// </param>
    /// <param name="config">
    /// Application Configuration.
    /// </param>
    /// <param name="serviceName">
    /// Cloud Foundry service name binding.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddOAuthServiceOptions(this IServiceCollection services, IConfiguration config, string serviceName)
    {
        ArgumentGuard.NotNull(services);

        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        ArgumentGuard.NotNull(config);

        var oauthConfig = new OAuthConnectorOptions(config);
        var info = config.GetRequiredServiceInfo<SsoServiceInfo>(serviceName);
        var factory = new OAuthConnectorFactory(info, oauthConfig);
        services.AddSingleton(typeof(IOptions<OAuthServiceOptions>), factory.Create);
        return services;
    }
}
