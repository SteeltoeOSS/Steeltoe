// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.Test.LoadBalancer;

/// <summary>
/// A fake load balancer that only resolves requests for "replace-me" to "some-resolved-host:1234".
/// </summary>
internal sealed class FakeLoadBalancer : ILoadBalancer
{
    internal IList<LoadBalancerStatistic> Statistics { get; } = [];

    public Task<Uri> ResolveServiceInstanceAsync(Uri requestUri, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(requestUri);

        string replacementUri = requestUri.AbsoluteUri.Replace("replace-me", "some-resolved-host:1234", StringComparison.Ordinal);
        return Task.FromResult(new Uri(replacementUri));
    }

    public Task UpdateStatisticsAsync(Uri requestUri, Uri serviceInstanceUri, TimeSpan? responseTime, Exception? exception, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(requestUri);
        ArgumentGuard.NotNull(serviceInstanceUri);

        Statistics.Add(new LoadBalancerStatistic(requestUri, serviceInstanceUri, responseTime, exception));
        return Task.CompletedTask;
    }
}
