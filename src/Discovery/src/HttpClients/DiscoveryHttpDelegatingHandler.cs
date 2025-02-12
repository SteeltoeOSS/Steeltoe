// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients;

/// <summary>
/// A <see cref="DelegatingHandler" /> that performs service discovery.
/// </summary>
/// <typeparam name="TLoadBalancer">
/// The type of load balancer to use.
/// </typeparam>
public sealed class DiscoveryHttpDelegatingHandler<TLoadBalancer> : DelegatingHandler
    where TLoadBalancer : class, ILoadBalancer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryHttpDelegatingHandler{TLoadBalancer}" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider" /> to obtain an instance of <typeparamref name="TLoadBalancer" /> from.
    /// </param>
    /// <param name="timeProvider">
    /// Provides access to the system time.
    /// </param>
    public DiscoveryHttpDelegatingHandler(IServiceProvider serviceProvider, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // We can't inject the load balancer, because a user may configure to use service discovery for *all* HTTP clients.
        // That would result in EurekaDiscoveryClient trying to use this handler for sending requests to Eureka, resulting in an infinite loop.
        // To prevent that from happening, EurekaDiscoveryClient excludes this handler from the chain for its own named HttpClient.
        var loadBalancer = _serviceProvider.GetService<TLoadBalancer>();

        if (loadBalancer == null)
        {
            throw new InvalidOperationException($"Please register your custom '{typeof(TLoadBalancer)}' load balancer in the IoC container.");
        }

        Uri? requestUri = request.RequestUri;
        Uri? serviceInstanceUri = null;
        Exception? error = null;
        DateTimeOffset startTime = _timeProvider.GetUtcNow();

        try
        {
            if (requestUri != null && requestUri.IsDefaultPort)
            {
                serviceInstanceUri = await loadBalancer.ResolveServiceInstanceAsync(requestUri, cancellationToken);
                request.RequestUri = serviceInstanceUri;
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            error = exception;
            throw;
        }
        finally
        {
            request.RequestUri = requestUri;

            if (requestUri != null && serviceInstanceUri != null && (error == null || !error.IsCancellation()))
            {
                DateTimeOffset endTime = _timeProvider.GetUtcNow();
                await loadBalancer.UpdateStatisticsAsync(requestUri, serviceInstanceUri, endTime - startTime, error, cancellationToken);
            }
        }
    }
}
