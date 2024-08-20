// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Configuration.CloudFoundry;

/// <summary>
/// Extension methods for adding services related to CloudFoundry.
/// </summary>
public static class CloudFoundryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="CloudFoundryApplicationOptions" /> and <see cref="CloudFoundryServicesOptions" /> for use with the options pattern. The first
    /// type is also registered as <see cref="IApplicationInstanceInfo" /> in the IoC container for easy access.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddCloudFoundryOptions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<CloudFoundryServicesOptions>().BindConfiguration("vcap");
        services.AddOptions<CloudFoundryApplicationOptions>().BindConfiguration(CloudFoundryApplicationOptions.ConfigurationPrefix);

        services.Replace(ServiceDescriptor.Singleton<IApplicationInstanceInfo>(serviceProvider =>
        {
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CloudFoundryApplicationOptions>>();
            return optionsMonitor.CurrentValue;
        }));

        return services;
    }
}
