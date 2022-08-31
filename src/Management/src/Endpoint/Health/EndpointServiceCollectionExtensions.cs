// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.Health;

public static class EndpointServiceCollectionExtensions
{
    internal static Type[] DefaultHealthContributors =>
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
    /// <param name="configuration">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided (this actuator looks for a settings starting with
    /// management:endpoints:health).
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, IConfiguration configuration = null)
    {
        services.AddHealthActuator(configuration, new HealthRegistrationsAggregator(), DefaultHealthContributors);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add health to.
    /// </param>
    /// <param name="configuration">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided (this actuator looks for a settings starting with
    /// management:endpoints:health).
    /// </param>
    /// <param name="contributors">
    /// Contributors to application health.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, IConfiguration configuration = null, params Type[] contributors)
    {
        ArgumentGuard.NotNull(services);

        services.AddHealthActuator(configuration, new HealthRegistrationsAggregator(), contributors);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add health to.
    /// </param>
    /// <param name="configuration">
    /// Application configuration. Retrieved from the <see cref="IServiceCollection" /> if not provided (this actuator looks for a settings starting with
    /// management:endpoints:health).
    /// </param>
    /// <param name="aggregator">
    /// Custom health aggregator.
    /// </param>
    /// <param name="contributors">
    /// Contributors to application health.
    /// </param>
    public static void AddHealthActuator(this IServiceCollection services, IConfiguration configuration, IHealthAggregator aggregator,
        params Type[] contributors)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(aggregator);

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.AddActuatorManagementOptions(configuration);
        services.AddHealthActuatorServices(configuration);

        AddHealthContributors(services, contributors);

        services.TryAddSingleton(aggregator);
        services.TryAddScoped<HealthEndpointCore>();
        services.AddActuatorEndpointMapping<HealthEndpointCore>();
        services.TryAddSingleton<ApplicationAvailability>();
    }

    public static void AddHealthContributors(IServiceCollection services, params Type[] contributors)
    {
        var descriptors = new List<ServiceDescriptor>();

        foreach (Type c in contributors)
        {
            descriptors.Add(new ServiceDescriptor(typeof(IHealthContributor), c, ServiceLifetime.Scoped));
        }

        services.TryAddEnumerable(descriptors);
    }
}
