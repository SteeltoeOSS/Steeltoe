using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Endpoint.Metrics;
internal class MetricCollectionHostedService : IHostedService
{
    private readonly AggregationManager aggregationManager;

    public MetricCollectionHostedService(AggregationManager aggregationManager)
    {
        this.aggregationManager = aggregationManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => aggregationManager.Start());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => aggregationManager.Dispose());
    }
}
