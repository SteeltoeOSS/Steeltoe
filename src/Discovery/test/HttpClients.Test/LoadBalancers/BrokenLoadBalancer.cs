// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

/// <summary>
/// A load balancer that always throws an exception.
/// </summary>
internal sealed class BrokenLoadBalancer : ILoadBalancer
{
    internal IList<LoadBalancerStatistic> Statistics { get; } = [];

    /// <summary>
    /// Throws an exception when you try to resolve service instances.
    /// </summary>
    public Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        throw new DataException("(╯°□°）╯︵ ┻━┻");
    }

    public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        ArgumentNullException.ThrowIfNull(serviceInstanceUri);

        cancellationToken.ThrowIfCancellationRequested();
        Statistics.Add(new LoadBalancerStatistic(requestUri, serviceInstanceUri, responseTime, exception));
        return Task.CompletedTask;
    }
}
