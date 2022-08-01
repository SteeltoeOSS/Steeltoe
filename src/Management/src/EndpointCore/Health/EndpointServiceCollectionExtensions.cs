// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Hypermedia;

namespace Steeltoe.Management.Endpoint.Health;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">Service collection to add health to.</param>
    /// <param name="config">Application configuration. Retrieved from the <see cref="IServiceCollection"/> if not provided (this actuator looks for a settings starting with management:endpoints:health).</param>
    public static void AddHealthActuator(this IServiceCollection services, IConfiguration config = null)
    {
        services.AddHealthActuator(config, new HealthRegistrationsAggregator(), DefaultHealthContributors);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">Service collection to add health to.</param>
    /// <param name="config">Application configuration. Retrieved from the <see cref="IServiceCollection"/> if not provided (this actuator looks for a settings starting with management:endpoints:health).</param>
    /// <param name="contributors">Contributors to application health.</param>
    public static void AddHealthActuator(this IServiceCollection services, IConfiguration config = null, params Type[] contributors)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddHealthActuator(config, new HealthRegistrationsAggregator(), contributors);
    }

    /// <summary>
    /// Adds components of the Health actuator to the D/I container.
    /// </summary>
    /// <param name="services">Service collection to add health to.</param>
    /// <param name="config">Application configuration. Retrieved from the <see cref="IServiceCollection"/> if not provided (this actuator looks for a settings starting with management:endpoints:health).</param>
    /// <param name="aggregator">Custom health aggregator.</param>
    /// <param name="contributors">Contributors to application health.</param>
    public static void AddHealthActuator(this IServiceCollection services, IConfiguration config, IHealthAggregator aggregator, params Type[] contributors)
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

        if (aggregator == null)
        {
            throw new ArgumentNullException(nameof(aggregator));
        }

        services.AddActuatorManagementOptions(config);
        services.AddHealthActuatorServices(config);

        AddHealthContributors(services, contributors);

        services.TryAddSingleton(aggregator);
        services.TryAddScoped<HealthEndpointCore>();
        services.AddActuatorEndpointMapping<HealthEndpointCore>();
        services.TryAddSingleton<ApplicationAvailability>();
    }

    public static void AddHealthContributors(IServiceCollection services, params Type[] contributors)
    {
        var descriptors = new List<ServiceDescriptor>();
        foreach (var c in contributors)
        {
            descriptors.Add(new ServiceDescriptor(typeof(IHealthContributor), c, ServiceLifetime.Scoped));
        }

        services.TryAddEnumerable(descriptors);
    }

    internal static Type[] DefaultHealthContributors => new[] { typeof(DiskSpaceContributor), typeof(LivenessHealthContributor), typeof(ReadinessHealthContributor) };
}
