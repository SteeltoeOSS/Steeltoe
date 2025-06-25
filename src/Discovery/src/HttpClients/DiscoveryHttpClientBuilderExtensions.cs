// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients;

public static class DiscoveryHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds service discovery for a named <see cref="HttpClient" /> using <see cref="RandomLoadBalancer" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddServiceDiscovery(this IHttpClientBuilder builder)
    {
        return AddServiceDiscovery<RandomLoadBalancer>(builder);
    }

    /// <summary>
    /// Adds service discovery for a named <see cref="HttpClient" /> using the specified load balancer.
    /// </summary>
    /// <typeparam name="TLoadBalancer">
    /// The type of load balancer to use, such as <see cref="RandomLoadBalancer" /> or <see cref="RoundRobinLoadBalancer" />.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IHttpClientBuilder" /> to configure an <see cref="HttpClient" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddServiceDiscovery<TLoadBalancer>(this IHttpClientBuilder builder)
        where TLoadBalancer : class, ILoadBalancer
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (typeof(TLoadBalancer) == typeof(RandomLoadBalancer) || typeof(TLoadBalancer) == typeof(RoundRobinLoadBalancer))
        {
            // The built-in load balancers are safe for concurrent usage. Any custom load balancer needs to be registered explicitly.
            builder.Services.TryAddSingleton<TLoadBalancer>();
            builder.Services.AddSingleton<ServiceInstancesResolver>();
        }

        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.AddHttpMessageHandler(serviceProvider =>
        {
            var timeProvider = serviceProvider.GetRequiredService<TimeProvider>();
            return new DiscoveryHttpDelegatingHandler<TLoadBalancer>(serviceProvider, timeProvider);
        });

        return builder;
    }
}
