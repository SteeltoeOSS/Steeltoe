// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Add services used by the CloudFoundry actuator
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Cloud Foundry actuator
    /// </summary>
    /// <param name="services">Reference to the service collection</param>
    /// <param name="configuration">Reference to the configuration system</param>
    /// <returns>A reference to the service collection</returns>
    public static IServiceCollection AddCloudFoundryActuatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new CloudFoundryManagementOptions(configuration)));
        services.TryAddSingleton(provider => provider.GetServices<IManagementOptions>().OfType<CloudFoundryManagementOptions>().First());

        services.TryAddSingleton<ICloudFoundryOptions>(new CloudFoundryEndpointOptions(configuration));

        services.TryAddSingleton(provider =>
        {
            var options = provider.GetService<ICloudFoundryOptions>();
            var mgmtOptions = provider.GetServices<IManagementOptions>().OfType<CloudFoundryManagementOptions>().SingleOrDefault();
            mgmtOptions.EndpointOptions.Add(options);

            return new CloudFoundryEndpoint(options, mgmtOptions);
        });
        services.TryAddSingleton<ICloudFoundryEndpoint>(provider => provider.GetRequiredService<CloudFoundryEndpoint>());

        return services;
    }
}