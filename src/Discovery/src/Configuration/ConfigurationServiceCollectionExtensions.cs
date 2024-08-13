// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Configures to use <see cref="ConfigurationDiscoveryClient" /> for service discovery. Reads service instances from app configuration, instead of a
    /// hosted service registry.
    /// </summary>
    /// <remarks>
    /// Build your list of service instances under the configuration prefix discovery:services.
    /// <example>
    /// Example configuration in appsettings.json:
    /// <![CDATA[
    /// {
    ///   "discovery": {
    ///     "services": [
    ///       {
    ///         "serviceId": "CartService",
    ///         "host": "knownhost1",
    ///         "port": 443,
    ///         "isSecure": true
    ///       }, {
    ///         "serviceId": "CartService",
    ///         "host": "knownhost2",
    ///         "port": 443,
    ///         "isSecure": true
    ///       },
    ///     ]
    ///   }
    /// }
    /// ]]>
    /// </example>
    /// </remarks>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddConfigurationDiscoveryClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ConfigurationDiscoveryOptions>().BindConfiguration(ConfigurationDiscoveryOptions.ConfigurationPrefix);

        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IDiscoveryClient), typeof(ConfigurationDiscoveryClient)));
        services.AddHostedService<DiscoveryClientHostedService>();

        return services;
    }
}
