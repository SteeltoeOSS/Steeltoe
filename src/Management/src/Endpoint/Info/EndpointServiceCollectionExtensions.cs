// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
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
    public static void AddInfoActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, GitInfoContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, AppSettingsInfoContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, BuildInfoContributor>());

        services.AddCommonActuatorServices();
        services.AddInfoActuatorServices();
    }

    /// <summary>
    /// Adds components of the info actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add info to.
    /// </param>
    /// <param name="contributors">
    /// Contributors to application information.
    /// </param>
    public static void AddInfoActuator(this IServiceCollection services, params IInfoContributor[] contributors)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentGuard.ElementsNotNull(contributors);

        AddContributors(services, contributors);
        services.AddInfoActuator();
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
