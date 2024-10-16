// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorCorsServiceCollectionExtensions
{
    internal const string PolicyName = "ActuatorsCorsPolicy";

    /// <summary>
    /// Configures a Cross-Origin Resource Sharing (CORS) policy for actuator endpoints. The policy applies to all actuator endpoints.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddActuatorsCorsPolicy(this IServiceCollection services)
    {
        return AddActuatorsCorsPolicy(services, null);
    }

    /// <summary>
    /// Configures a Cross-Origin Resource Sharing (CORS) policy for actuator endpoints. The policy applies to all actuator endpoints.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureCorsPolicy">
    /// Enables to further configure the actuators policy. Because the policy applies to all endpoints, this overload must be called before adding actuators.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddActuatorsCorsPolicy(this IServiceCollection services, Action<CorsPolicyBuilder>? configureCorsPolicy)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<CorsOptions>().Configure<IEnumerable<IEndpointOptionsMonitorProvider>>((options, endpointOptionsMonitorProviders) =>
        {
            if (options.GetPolicy(PolicyName) != null)
            {
                if (configureCorsPolicy != null)
                {
                    throw new InvalidOperationException(
                        $"A CORS policy for actuator endpoints has already been configured. Call '{nameof(AddActuatorsCorsPolicy)}()' before adding actuators.");
                }

                return;
            }

            options.AddPolicy(PolicyName, policyBuilder =>
            {
                string[] methods = GetEndpointMethods(endpointOptionsMonitorProviders);
                policyBuilder.WithMethods(methods);

                if (Platform.IsCloudFoundry)
                {
                    policyBuilder.WithHeaders("Authorization", "X-Cf-App-Instance", "Content-Type", "Content-Disposition");
                }

                if (configureCorsPolicy != null)
                {
                    configureCorsPolicy(policyBuilder);
                }
                else
                {
                    policyBuilder.AllowAnyOrigin();
                }
            });
        });

        return services;
    }

    private static string[] GetEndpointMethods(IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders)
    {
        HashSet<string> upperCaseMethods = [];

        foreach (string method in endpointOptionsMonitorProviders.Select(provider => provider.Get())
            .SelectMany(endpointOptions => endpointOptions.GetSafeAllowedVerbs()))
        {
            upperCaseMethods.Add(method.ToUpperInvariant());
        }

        return upperCaseMethods.ToArray();
    }
}
