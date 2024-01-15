// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Discovery;

namespace Steeltoe.Common.Http.LoadBalancer;

public static class LoadBalancerHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> that performs random load balancing.
    /// </summary>
    /// <param name="httpClientBuilder">
    /// The <see cref="IHttpClientBuilder" />.
    /// </param>
    /// <remarks>
    /// Requires an <see cref="IServiceInstanceProvider" /> or <see cref="IDiscoveryClient" /> in the DI container so the load balancer can send traffic to
    /// more than one address.
    /// </remarks>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    public static IHttpClientBuilder AddRandomLoadBalancer(this IHttpClientBuilder httpClientBuilder)
    {
        ArgumentGuard.NotNull(httpClientBuilder);

        httpClientBuilder.Services.TryAddSingleton<RandomLoadBalancer>();
        return httpClientBuilder.AddLoadBalancer<RandomLoadBalancer>();
    }

    /// <summary>
    /// Adds a <see cref="DelegatingHandler" /> that performs round-robin load balancing, optionally backed by an <see cref="IDistributedCache" />.
    /// </summary>
    /// <param name="httpClientBuilder">
    /// The <see cref="IHttpClientBuilder" />.
    /// </param>
    /// <remarks>
    /// Requires an <see cref="IServiceInstanceProvider" /> or <see cref="IDiscoveryClient" /> in the DI container so the load balancer can send traffic to
    /// more than one address.
    /// </remarks>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    public static IHttpClientBuilder AddRoundRobinLoadBalancer(this IHttpClientBuilder httpClientBuilder)
    {
        ArgumentGuard.NotNull(httpClientBuilder);

        httpClientBuilder.Services.TryAddSingleton<RoundRobinLoadBalancer>();
        return httpClientBuilder.AddLoadBalancer<RoundRobinLoadBalancer>();
    }

    /// <summary>
    /// Adds an <see cref="HttpMessageHandler" /> with the specified load balancer.
    /// <para />
    /// Does NOT add the specified load balancer to the container. Please add your load balancer separately.
    /// </summary>
    /// <param name="httpClientBuilder">
    /// The <see cref="IHttpClientBuilder" />.
    /// </param>
    /// <typeparam name="T">
    /// The type of <see cref="ILoadBalancer" /> to use.
    /// </typeparam>
    /// <returns>
    /// An <see cref="IHttpClientBuilder" /> that can be used to configure the client.
    /// </returns>
    public static IHttpClientBuilder AddLoadBalancer<T>(this IHttpClientBuilder httpClientBuilder)
        where T : ILoadBalancer
    {
        ArgumentGuard.NotNull(httpClientBuilder);

        httpClientBuilder.AddHttpMessageHandler(serviceProvider => new LoadBalancerDelegatingHandler(serviceProvider.GetRequiredService<T>()));
        return httpClientBuilder;
    }
}
