// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.LoadBalancer;

/// <summary>
/// Same as <see cref="LoadBalancerDelegatingHandler" /> except is an <see cref="HttpClientHandler" />, for non-HttpClientFactory use.
/// </summary>
public class LoadBalancerHttpClientHandler : HttpClientHandler
{
    private readonly ILoadBalancer _loadBalancer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancerHttpClientHandler" /> class.
    /// <para />
    /// For use with <see cref="HttpClient" /> without <see cref="IHttpClientFactory" />.
    /// </summary>
    /// <param name="loadBalancer">
    /// Load balancer to use.
    /// </param>
    public LoadBalancerHttpClientHandler(ILoadBalancer loadBalancer)
    {
        _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancerHttpClientHandler" /> class.
    /// <para />
    /// For use with <see cref="HttpClient" /> without <see cref="IHttpClientFactory" />.
    /// </summary>
    /// <param name="loadBalancer">
    /// Load balancer to use.
    /// </param>
    /// <param name="logger">
    /// For logging.
    /// </param>
    [Obsolete("Please remove ILogger parameter")]
    public LoadBalancerHttpClientHandler(ILoadBalancer loadBalancer, ILogger logger)
    {
        _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // record the original request
        Uri originalUri = request.RequestUri;

        // look up a service instance and update the request
        Uri resolvedUri = await _loadBalancer.ResolveServiceInstanceAsync(request.RequestUri).ConfigureAwait(false);
        request.RequestUri = resolvedUri;

        // allow other handlers to operate and the request to continue
        DateTime startTime = DateTime.UtcNow;

        Exception exception = null;

        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            exception = ex;

            throw;
        }
        finally
        {
            request.RequestUri = originalUri;

            // track stats
            await _loadBalancer.UpdateStatsAsync(originalUri, resolvedUri, DateTime.UtcNow - startTime, exception).ConfigureAwait(false);
        }
    }
}
