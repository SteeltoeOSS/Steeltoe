// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Steeltoe.Management.MetricCollectors.Aggregations;

namespace Steeltoe.Management.Endpoint.Metrics;

internal sealed class MetricCollectionHostedService : IHostedService
{
    private readonly AggregationManager _aggregationManager;

    public MetricCollectionHostedService(AggregationManager aggregationManager)
    {
        _aggregationManager = aggregationManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(_aggregationManager.Start, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.Run(_aggregationManager.Dispose, cancellationToken);
    }
}
