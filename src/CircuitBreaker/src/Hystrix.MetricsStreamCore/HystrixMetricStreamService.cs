// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream;

public class HystrixMetricStreamService : IHostedService
{
    private readonly RabbitMetricsStreamPublisher _streamPublisher;

    public HystrixMetricStreamService(RabbitMetricsStreamPublisher streamPublisher)
    {
        _streamPublisher = streamPublisher;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}