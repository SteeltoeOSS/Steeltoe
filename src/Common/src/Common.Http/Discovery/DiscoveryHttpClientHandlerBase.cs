// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.Discovery;

internal sealed class DiscoveryHttpClientHandlerBase
{
    private readonly ILoadBalancer _loadBalancer;
    private readonly ILogger _logger;

    public DiscoveryHttpClientHandlerBase(IDiscoveryClient discoveryClient, ILoggerFactory loggerFactory, ILoadBalancer? loadBalancer = null)
    {
        ArgumentGuard.NotNull(discoveryClient);
        ArgumentGuard.NotNull(loggerFactory);

        _loadBalancer = loadBalancer ?? new RandomLoadBalancer(discoveryClient, loggerFactory.CreateLogger<RandomLoadBalancer>());
        _logger = loggerFactory.CreateLogger<DiscoveryHttpClientHandlerBase>();
    }

    public async Task<Uri> LookupServiceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        _logger.LogDebug("LookupService({uri})", requestUri);

        if (!requestUri.IsDefaultPort)
        {
            return requestUri;
        }

        return await _loadBalancer.ResolveServiceInstanceAsync(requestUri, cancellationToken);
    }
}
