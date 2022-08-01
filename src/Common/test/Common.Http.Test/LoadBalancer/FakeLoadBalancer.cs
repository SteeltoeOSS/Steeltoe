// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.LoadBalancer.Test;

/// <summary>
/// A bad fake load balancer that only resolves requests for "replaceme" as "someresolvedhost".
/// </summary>
internal sealed class FakeLoadBalancer : ILoadBalancer
{
    internal List<Tuple<Uri, Uri, TimeSpan, Exception>> Stats = new ();

    public Task<Uri> ResolveServiceInstanceAsync(Uri request)
    {
        return Task.FromResult(new Uri(request.AbsoluteUri.Replace("replaceme", "someresolvedhost")));
    }

    public Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
    {
        Stats.Add(new Tuple<Uri, Uri, TimeSpan, Exception>(originalUri, resolvedUri, responseTime, exception));
        return Task.CompletedTask;
    }
}
