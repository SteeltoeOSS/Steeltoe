// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Actuators.Health.Contributors;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the health actuator to the service container and configures the ASP.NET middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddHealthActuator(this IServiceCollection services)
    {
        return AddHealthActuator(services, true);
    }

    /// <summary>
    /// Adds the health actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddHealthActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCoreActuatorServices<HealthEndpointOptions, ConfigureHealthEndpointOptions, HealthEndpointMiddleware, IHealthEndpointHandler,
            HealthEndpointHandler, HealthEndpointRequest, HealthEndpointResponse>(configureMiddleware);

        RegisterJsonConverter(services);

        services.TryAddSingleton<IHealthAggregator, HealthAggregator>();
        RegisterDefaultHealthContributors(services);

        return services;
    }

    private static void RegisterJsonConverter(IServiceCollection services)
    {
        services.PostConfigure<ManagementOptions>(managementOptions =>
        {
            if (!managementOptions.SerializerOptions.Converters.Any(converter => converter is HealthConverter))
            {
                managementOptions.SerializerOptions.Converters.Add(new HealthConverter());
            }
        });
    }

    private static void RegisterDefaultHealthContributors(IServiceCollection services)
    {
        services.ConfigureOptionsWithChangeTokenSource<DiskSpaceContributorOptions, ConfigureDiskSpaceContributorOptions>();
        AddHealthContributor<DiskSpaceContributor>(services);

        services.TryAddSingleton<ApplicationAvailability>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, AvailabilityStartupFilter>());

        services.ConfigureOptionsWithChangeTokenSource<LivenessHealthContributorOptions, ConfigureLivenessHealthContributorOptions>();
        AddHealthContributor<LivenessHealthContributor>(services);

        services.ConfigureOptionsWithChangeTokenSource<ReadinessHealthContributorOptions, ConfigureReadinessHealthContributorOptions>();
        AddHealthContributor<ReadinessHealthContributor>(services);
    }

    /// <summary>
    /// Adds the specified <see cref="IHealthContributor" /> to the D/I container as a scoped service.
    /// </summary>
    /// <typeparam name="T">
    /// The type of health contributor to add.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddHealthContributor<T>(this IServiceCollection services)
        where T : class, IHealthContributor
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHealthContributor, T>());

        return services;
    }

    /// <summary>
    /// Adds the specified <see cref="IHealthContributor" /> to the D/I container as a scoped service.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="healthContributorType">
    /// The type of the health contributor to add.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddHealthContributor(this IServiceCollection services, Type healthContributorType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(healthContributorType);

        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHealthContributor), healthContributorType));

        return services;
    }
}
