// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;

namespace Steeltoe.Management.Endpoint.Trace;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the trace actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddTraceActuator(this IServiceCollection services)
    {
        return AddTraceActuator(services, MediaTypeVersion.V2);
    }

    /// <summary>
    /// Adds the trace actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="version">
    /// <see cref="MediaTypeVersion" /> to use in responses.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddTraceActuator(this IServiceCollection services, MediaTypeVersion version)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.AddHostedService<DiagnosticsService>();

        services.AddCommonActuatorServices();
        services.AddTraceActuatorServices(version);

        switch (version)
        {
            case MediaTypeVersion.V1:

                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, TraceDiagnosticObserver>());

                services.TryAddSingleton<IHttpTraceRepository>(provider =>
                    provider.GetServices<IDiagnosticObserver>().OfType<TraceDiagnosticObserver>().Single());

                break;
            default:
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpTraceDiagnosticObserver>());

                services.TryAddSingleton<IHttpTraceRepository>(provider =>
                    provider.GetServices<IDiagnosticObserver>().OfType<HttpTraceDiagnosticObserver>().Single());

                break;
        }

        return services;
    }
}
