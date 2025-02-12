// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients;

/// <summary>
/// A <see cref="HttpClientHandler" /> that performs service discovery.
/// </summary>
public sealed class DiscoveryHttpClientHandler : HttpClientHandler
{
    private readonly ILoadBalancer _loadBalancer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryHttpClientHandler" /> class.
    /// </summary>
    /// <param name="loadBalancer">
    /// The load balancer to use.
    /// </param>
    /// <param name="timeProvider">
    /// Provides access to the system time.
    /// </param>
    public DiscoveryHttpClientHandler(ILoadBalancer loadBalancer, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(loadBalancer);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _loadBalancer = loadBalancer;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Uri? requestUri = request.RequestUri;
        Uri? serviceInstanceUri = null;
        Exception? error = null;
        DateTimeOffset startTime = _timeProvider.GetUtcNow();

        try
        {
            if (requestUri != null && requestUri.IsDefaultPort)
            {
                serviceInstanceUri = await _loadBalancer.ResolveServiceInstanceAsync(requestUri, cancellationToken);
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
                await _loadBalancer.UpdateStatisticsAsync(requestUri, serviceInstanceUri, endTime - startTime, error, cancellationToken);
            }
        }
    }
}
