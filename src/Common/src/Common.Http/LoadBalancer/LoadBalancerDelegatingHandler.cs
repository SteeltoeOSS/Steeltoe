// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.LoadBalancer;

/// <summary>
/// Same as <see cref="LoadBalancerHttpClientHandler" />, except this is a <see cref="DelegatingHandler" />, for use with
/// <see cref="IHttpClientFactory" />.
/// </summary>
public sealed class LoadBalancerDelegatingHandler : DelegatingHandler
{
    private readonly ILoadBalancer _loadBalancer;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancerDelegatingHandler" /> class.
    /// <para />
    /// For use with <see cref="IHttpClientBuilder" />.
    /// </summary>
    /// <param name="loadBalancer">
    /// Load balancer to use.
    /// </param>
    public LoadBalancerDelegatingHandler(ILoadBalancer loadBalancer)
    {
        ArgumentGuard.NotNull(loadBalancer);

        _loadBalancer = loadBalancer;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(request);

        return await LoadBalancerHttpClientHandler.InternalSendAsync(request, _loadBalancer, base.SendAsync, cancellationToken);
    }
}
