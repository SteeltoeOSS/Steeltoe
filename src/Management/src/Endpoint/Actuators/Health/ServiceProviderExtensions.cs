// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Register events to trigger initial and shutting down values for Readiness and Liveness states of <see cref="ApplicationAvailability" />.
    /// </summary>
    /// <param name="serviceProvider">
    /// The application's configured services.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="serviceProvider" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceProvider InitializeAvailability(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var availability = serviceProvider.GetService<ApplicationAvailability>();

        if (availability != null)
        {
            var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

            lifetime.ApplicationStarted.Register(() =>
            {
                availability.SetAvailabilityState(ApplicationAvailability.LivenessKey, LivenessState.Correct, "ApplicationStarted");
                availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.AcceptingTraffic, "ApplicationStarted");
            });

            lifetime.ApplicationStopping.Register(() =>
                availability.SetAvailabilityState(ApplicationAvailability.ReadinessKey, ReadinessState.RefusingTraffic, "ApplicationStopping"));
        }

        return serviceProvider;
    }
}
