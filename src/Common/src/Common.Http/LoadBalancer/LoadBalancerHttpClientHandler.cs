// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.LoadBalancer;

/// <summary>
/// Same as <see cref="LoadBalancerDelegatingHandler" />, except this is an <see cref="HttpClientHandler" />, for use with <see cref="HttpClient" />
/// without <see cref="IHttpClientFactory" />.
/// </summary>
public sealed class LoadBalancerHttpClientHandler : HttpClientHandler
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
        ArgumentGuard.NotNull(loadBalancer);

        _loadBalancer = loadBalancer;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(request);

        return await InternalSendAsync(request, _loadBalancer, base.SendAsync, cancellationToken);
    }

    internal static async Task<HttpResponseMessage> InternalSendAsync(HttpRequestMessage request, ILoadBalancer loadBalancer,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> innerSendAsync, CancellationToken cancellationToken)
    {
        // record the original request
        Uri requestUri = request.RequestUri!;

        // look up a service instance and update the request
        Uri serviceInstanceUri = await loadBalancer.ResolveServiceInstanceAsync(requestUri, cancellationToken);
        request.RequestUri = serviceInstanceUri;

        // allow other handlers to operate and the request to continue
        DateTime startTime = DateTime.UtcNow;

        Exception? error = null;

        try
        {
            return await innerSendAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            error = exception;
            throw;
        }
        finally
        {
            request.RequestUri = requestUri;
            await loadBalancer.UpdateStatisticsAsync(requestUri, serviceInstanceUri, DateTime.UtcNow - startTime, error, cancellationToken);
        }
    }
}
