// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Info actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add info to.
    /// </param>
    /// <param name="config">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided (this actuator looks for a settings starting with
    /// management:endpoints:info).
    /// </param>
    public static void AddInfoActuator(this IServiceCollection services, IConfiguration config = null)
    {
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        config ??= serviceProvider.GetRequiredService<IConfiguration>();
        IEnumerable<IInfoContributor> otherInfoContributors = serviceProvider.GetServices<IInfoContributor>();

        var allContributors = new List<IInfoContributor>
        {
            new GitInfoContributor(),
            new AppSettingsInfoContributor(config),
            new BuildInfoContributor()
        };

        foreach (IInfoContributor o in otherInfoContributors)
        {
            allContributors.Add(o);
        }

        services.AddInfoActuator(config, allContributors.ToArray());
    }

    /// <summary>
    /// Adds components of the info actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add info to.
    /// </param>
    /// <param name="config">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided (this actuator looks for a settings starting with
    /// management:endpoints:info).
    /// </param>
    /// <param name="contributors">
    /// Contributors to application information.
    /// </param>
    public static void AddInfoActuator(this IServiceCollection services, IConfiguration config = null, params IInfoContributor[] contributors)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        config ??= services.BuildServiceProvider().GetService<IConfiguration>();

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        services.AddActuatorManagementOptions(config);
        services.AddInfoActuatorServices(config);
        services.AddActuatorEndpointMapping<InfoEndpoint>();

        AddContributors(services, contributors);
    }

    private static void AddContributors(IServiceCollection services, params IInfoContributor[] contributors)
    {
        var descriptors = new List<ServiceDescriptor>();

        foreach (IInfoContributor instance in contributors)
        {
            descriptors.Add(ServiceDescriptor.Singleton(instance));
        }

        services.TryAddEnumerable(descriptors);
    }
}
