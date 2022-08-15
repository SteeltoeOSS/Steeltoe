// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steeltoe.Management.Endpoint.Loggers;

/// <summary>
/// Add services used by the Loggers actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Loggers actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddLoggersActuatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var options = new LoggersEndpointOptions(configuration);
        services.TryAddSingleton<ILoggersOptions>(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
        services.TryAddSingleton<LoggersEndpoint>();
        services.TryAddSingleton<ILoggersEndpoint>(provider => provider.GetRequiredService<LoggersEndpoint>());

        return services;
    }
}
