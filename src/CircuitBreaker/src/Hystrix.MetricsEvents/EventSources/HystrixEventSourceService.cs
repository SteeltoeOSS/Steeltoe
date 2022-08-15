// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Hosting;
using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using static Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.HystrixDashboardStream;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEvents.EventSources;

public class HystrixEventSourceService : IHostedService
{
    protected internal IDisposable SampleSubscription { get; set; }

    public HystrixDashboardStream Stream { get; }

    public HystrixEventSourceService(HystrixDashboardStream stream)
    {
        Stream = stream;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Subscribe();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal void OnNext(DashboardData dashboardData)
    {
        if (dashboardData != null)
        {
            try
            {
                foreach (HystrixCommandMetrics commandMetrics in dashboardData.CommandMetrics)
                {
                    ICircuitBreaker circuitBreaker = HystrixCircuitBreakerFactory.GetInstance(commandMetrics.CommandKey);
                    bool? isOpen = circuitBreaker?.IsOpen;

                    HystrixMetricsEventSource.EventLogger.CommandMetrics(commandMetrics.CommandKey.Name, commandMetrics.CommandGroup.Name,
                        isOpen.HasValue && isOpen.Value, commandMetrics.HealthCounts.ErrorCount, commandMetrics.HealthCounts.TotalRequests,
                        commandMetrics.CurrentConcurrentExecutionCount, commandMetrics.ExecutionTimeMean, commandMetrics.TotalTimeMean,
                        1, // this will get summed across all instances in a cluster
                        commandMetrics.ThreadPoolKey.Name);
                }

                foreach (HystrixThreadPoolMetrics threadPoolMetrics in dashboardData.ThreadPoolMetrics)
                {
                    HystrixMetricsEventSource.EventLogger.ThreadPoolMetrics(threadPoolMetrics.ThreadPoolKey.Name,
                        threadPoolMetrics.CumulativeCountThreadsExecuted, threadPoolMetrics.CurrentActiveCount, threadPoolMetrics.CurrentCompletedTaskCount,
                        threadPoolMetrics.CurrentCorePoolSize, threadPoolMetrics.CurrentLargestPoolSize, threadPoolMetrics.CurrentMaximumPoolSize,
                        threadPoolMetrics.CurrentPoolSize, threadPoolMetrics.CurrentQueueSize, threadPoolMetrics.CurrentTaskCount,
                        1); // this will get summed across all instances in a cluster
                }

                foreach (HystrixCollapserMetrics collapserMetrics in dashboardData.CollapserMetrics)
                {
                    HystrixMetricsEventSource.EventLogger.CollapserMetrics(collapserMetrics.CollapserKey.Name,
                        collapserMetrics.GetRollingCount(CollapserEventType.AddedToBatch), collapserMetrics.GetRollingCount(CollapserEventType.BatchExecuted),
                        collapserMetrics.GetRollingCount(CollapserEventType.ResponseFromCache), collapserMetrics.BatchSizeMean,
                        1); // this will get summed across all instances in a cluster
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private void Subscribe()
    {
        SampleSubscription = Stream.Observe().ObserveOn(NewThreadScheduler.Default).Subscribe(OnNext, ReSubscribeOnError, ReSubscribe);
    }

    private void ReSubscribeOnError(Exception ex)
    {
        ReSubscribe();
    }

    private void ReSubscribe()
    {
        if (SampleSubscription != null)
        {
            SampleSubscription.Dispose();
            SampleSubscription = null;
        }

        Subscribe();
    }
}
