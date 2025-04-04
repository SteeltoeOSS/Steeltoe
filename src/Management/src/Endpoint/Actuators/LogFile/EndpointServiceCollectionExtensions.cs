// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Endpoint.Actuators.Logfile;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the logfile actuator to the service container and configure the ASP.NET Core middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddLogFileActuator(this IServiceCollection services)
    {
        return AddLogFileActuator(services, true);
    }

    /// <summary>
    /// Adds the logfile actuator to the service container.
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
    public static IServiceCollection AddLogFileActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IHostEnvironment>(serviceProvider => serviceProvider.GetService<IHostEnvironment>()!);

        services.AddCoreActuatorServices<LogFileEndpointOptions, ConfigureLogFileEndpointOptions, LogFileEndpointMiddleware,
            ILogFileEndpointHandler, LogFileEndpointHandler, object?, string>(configureMiddleware);

        return services;
    }
}
