// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients;

/// <summary>
/// A <see cref="HttpClientHandler" /> that performs service discovery.
/// </summary>
public sealed class DiscoveryHttpClientHandler : HttpClientHandler
{
    private readonly ILoadBalancer _loadBalancer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryHttpClientHandler" /> class.
    /// </summary>
    /// <param name="loadBalancer">
    /// The load balancer to use.
    /// </param>
    public DiscoveryHttpClientHandler(ILoadBalancer loadBalancer)
    {
        ArgumentGuard.NotNull(loadBalancer);

        _loadBalancer = loadBalancer;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(request);

        Uri? requestUri = request.RequestUri;
        Uri? serviceInstanceUri = null;
        Exception? error = null;
        DateTime startTime = DateTime.UtcNow;

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
                await _loadBalancer.UpdateStatisticsAsync(requestUri, serviceInstanceUri, DateTime.UtcNow - startTime, error, cancellationToken);
            }
        }
    }
}
