// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Client.SimpleClients;

namespace Steeltoe.Discovery.Client;

public static class ConfigurationDiscoveryClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds a discovery client that reads service instances from configuration.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddConfigurationDiscoveryClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        services.AddSingleton<IDiscoveryClient, ConfigurationDiscoveryClient>();
        services.AddOptions();
        services.Configure<List<ConfigurationServiceInstance>>(configuration.GetSection("discovery:services"));
        return services;
    }
}
