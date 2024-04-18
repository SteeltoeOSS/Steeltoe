// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients;

public static class DiscoveryHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds service discovery for a named <see cref="HttpClient" /> using <see cref="RandomLoadBalancer" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="HttpClient" /> for a named <see cref="IHttpClientBuilder" />.
    /// </param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
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
    /// The <see cref="IHttpClientBuilder" /> for a named <see cref="HttpClient" />.
    /// </param>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    public static IHttpClientBuilder AddServiceDiscovery<TLoadBalancer>(this IHttpClientBuilder builder)
        where TLoadBalancer : class, ILoadBalancer
    {
        ArgumentGuard.NotNull(builder);

        if (typeof(TLoadBalancer) == typeof(RandomLoadBalancer) || typeof(TLoadBalancer) == typeof(RoundRobinLoadBalancer))
        {
            // The built-in load balancers are safe for concurrent usage. Any custom load balancer needs to be registered explicitly.
            builder.Services.TryAddSingleton<TLoadBalancer>();
        }

        builder.AddHttpMessageHandler(serviceProvider => new DiscoveryHttpDelegatingHandler<TLoadBalancer>(serviceProvider));

        return builder;
    }
}
