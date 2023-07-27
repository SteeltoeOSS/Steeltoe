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
    private static Type[] DefaultHealthContributors =>
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
        AddHealthActuator(services, DefaultHealthContributors);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add health to.
    /// </param>
    /// <param name="contributors">
    /// Contributors to application health.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, params Type[] contributors)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(contributors);

        services.AddHealthActuator(new HealthRegistrationsAggregator(), contributors);
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
    /// <param name="contributors">
    /// Contributors to application health.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, IHealthAggregator aggregator, params Type[] contributors)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(aggregator);
        ArgumentGuard.NotNull(contributors);

        services.AddCommonActuatorServices();
        services.AddHealthActuatorServices();

        AddHealthContributors(services, contributors);

        services.TryAddSingleton(aggregator);
        services.TryAddSingleton<ApplicationAvailability>();
    }

    public static void AddHealthContributors(this IServiceCollection services, params Type[] contributors)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(contributors);

        var descriptors = new List<ServiceDescriptor>();

        foreach (Type c in contributors)
        {
            descriptors.Add(new ServiceDescriptor(typeof(IHealthContributor), c, ServiceLifetime.Scoped));
        }

        services.TryAddEnumerable(descriptors);
    }
}
