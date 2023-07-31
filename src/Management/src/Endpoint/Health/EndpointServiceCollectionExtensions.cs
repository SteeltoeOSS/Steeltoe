// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;

namespace Steeltoe.Management.Endpoint.Health;

public static class EndpointServiceCollectionExtensions
{
    private static Type[] DefaultHealthContributorTypes =>
        new[]
        {
            typeof(DiskSpaceContributor),
            typeof(LivenessHealthContributor),
            typeof(ReadinessHealthContributor)
        };

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add health to.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services)
    {
        AddHealthActuator(services, DefaultHealthContributorTypes);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add health to.
    /// </param>
    /// <param name="contributorTypes">
    /// Contributors to application health.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(contributorTypes);

        services.AddHealthActuator(new HealthRegistrationsAggregator(), contributorTypes);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add health to.
    /// </param>
    /// <param name="aggregator">
    /// Custom health aggregator.
    /// </param>
    /// <param name="contributorTypes">
    /// Contributors to application health.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, IHealthAggregator aggregator, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributorTypes);

        services.AddCommonActuatorServices();
        services.AddHealthActuatorServices();

        AddHealthContributors(services, contributorTypes);

        services.TryAddSingleton(aggregator);
        services.TryAddSingleton<ApplicationAvailability>();
    }

    public static void AddHealthContributors(this IServiceCollection services, params Type[] contributorTypes)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(contributorTypes);

        var descriptors = new List<ServiceDescriptor>();

        foreach (Type contributorType in contributorTypes)
        {
            descriptors.Add(new ServiceDescriptor(typeof(IHealthContributor), contributorType, ServiceLifetime.Scoped));
        }

        services.TryAddEnumerable(descriptors);
    }
}
