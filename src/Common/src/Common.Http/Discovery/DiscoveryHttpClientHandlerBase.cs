// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Discovery;

namespace Steeltoe.Common.Http.Discovery;

public class DiscoveryHttpClientHandlerBase
{
    protected IDiscoveryClient client;
    protected ILoadBalancer loadBalancer;
    protected ILogger logger;

    public DiscoveryHttpClientHandlerBase(IDiscoveryClient client, ILogger logger = null, ILoadBalancer loadBalancer = null)
    {
        ArgumentGuard.NotNull(client);

        this.client = client;
        this.loadBalancer = loadBalancer ?? new RandomLoadBalancer(client);
        this.logger = logger;
    }

    public virtual async Task<Uri> LookupServiceAsync(Uri current, CancellationToken cancellationToken)
    {
        logger?.LogDebug("LookupService({uri})", current);

        if (!current.IsDefaultPort)
        {
            return current;
        }

        return await loadBalancer.ResolveServiceInstanceAsync(current, cancellationToken);
    }
}
