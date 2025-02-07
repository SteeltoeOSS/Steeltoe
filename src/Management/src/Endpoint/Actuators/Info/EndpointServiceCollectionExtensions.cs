// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

namespace Steeltoe.Management.Endpoint.Actuators.Info;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the info actuator to the service container and configures the ASP.NET Core middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddInfoActuator(this IServiceCollection services)
    {
        return AddInfoActuator(services, true);
    }

    /// <summary>
    /// Adds the info actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET Core middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddInfoActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCoreActuatorServices<InfoEndpointOptions, ConfigureInfoEndpointOptions, InfoEndpointMiddleware, IInfoEndpointHandler,
            InfoEndpointHandler, object?, IDictionary<string, object>>(configureMiddleware);

        RegisterDefaultInfoContributors(services);

        return services;
    }

    private static void RegisterDefaultInfoContributors(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, GitInfoContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, AppSettingsInfoContributor>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, BuildInfoContributor>());
    }

    /// <summary>
    /// Adds the specified <see cref="IInfoContributor" /> to the D/I container as a singleton service.
    /// </summary>
    /// <typeparam name="T">
    /// The type of info contributor to add.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddInfoContributor<T>(this IServiceCollection services)
        where T : class, IInfoContributor
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IInfoContributor, T>());

        return services;
    }

    /// <summary>
    /// Adds the specified <see cref="IInfoContributor" /> to the D/I container as a singleton service.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="infoContributorType">
    /// The type of the info contributor to add.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddInfoContributor(this IServiceCollection services, Type infoContributorType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(infoContributorType);

        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IInfoContributor), infoContributorType));

        return services;
    }
}
