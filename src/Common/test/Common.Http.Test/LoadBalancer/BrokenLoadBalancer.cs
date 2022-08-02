// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.LoadBalancer;

namespace Steeltoe.Common.Http.LoadBalancer.Test;

internal sealed class BrokenLoadBalancer : ILoadBalancer
{
    internal List<Tuple<Uri, Uri, TimeSpan, Exception>> Stats = new();

    /// <summary>
    /// Throws exceptions when you try to resolve services.
    /// </summary>
    public Task<Uri> ResolveServiceInstanceAsync(Uri request)
    {
        throw new Exception("(╯°□°）╯︵ ┻━┻");
    }

    public Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
    {
        Stats.Add(new Tuple<Uri, Uri, TimeSpan, Exception>(originalUri, resolvedUri, responseTime, exception));
        return Task.CompletedTask;
    }
}
