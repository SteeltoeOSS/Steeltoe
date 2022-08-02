// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Trace;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Add services used by the Trace actuator.
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Trace actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <param name="version">
    /// The media version to use.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddTraceActuatorServices(this IServiceCollection services, IConfiguration configuration, MediaTypeVersion version)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        switch (version)
        {
            case MediaTypeVersion.V1:
                var options = new TraceEndpointOptions(configuration);
                services.TryAddSingleton<ITraceOptions>(options);
                services.TryAddSingleton<TraceEndpoint>();
                services.TryAddSingleton<ITraceEndpoint>(provider => provider.GetRequiredService<TraceEndpoint>());
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
                break;
            default:
                var options2 = new HttpTraceEndpointOptions(configuration);
                services.TryAddSingleton<ITraceOptions>(options2);
                services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options2));
                break;
        }

        return services;
    }
}
