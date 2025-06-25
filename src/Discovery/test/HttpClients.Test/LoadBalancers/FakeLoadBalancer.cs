// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.HttpClients.LoadBalancers;

namespace Steeltoe.Discovery.HttpClients.Test.LoadBalancers;

/// <summary>
/// A fake load balancer that only resolves requests for "replace-me" to "some-resolved-host:1234".
/// </summary>
internal sealed class FakeLoadBalancer : ILoadBalancer
{
    internal IList<LoadBalancerStatistic> Statistics { get; } = [];

    public Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestUri);

        string replacementUri = requestUri.AbsoluteUri.Replace("replace-me", "some-resolved-host:1234", StringComparison.Ordinal);
        return Task.FromResult(new Uri(replacementUri));
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
