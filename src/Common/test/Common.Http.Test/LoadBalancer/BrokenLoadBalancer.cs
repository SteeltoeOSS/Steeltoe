// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.Test.LoadBalancer;

/// <summary>
/// A load balancer that always throws an exception.
/// </summary>
internal sealed class BrokenLoadBalancer : ILoadBalancer
{
    internal IList<LoadBalancerStatistic> Statistics { get; } = [];

    /// <summary>
    /// Throws exceptions when you try to resolve services.
    /// </summary>
    public Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        throw new Exception("(╯°□°）╯︵ ┻━┻");
    }

    public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(requestUri);
        ArgumentGuard.NotNull(serviceInstanceUri);

        Statistics.Add(new LoadBalancerStatistic(requestUri, serviceInstanceUri, responseTime, exception));
        return Task.CompletedTask;
    }
}
