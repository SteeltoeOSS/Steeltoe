using Microsoft.Extensions.Hosting;
using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsEventsCore.EventSources
{
    public class EventSourceManager : IHostedService
    {

        public HystrixDashboardStream Stream { get; }

        public EventSourceManager(HystrixDashboardStream stream)
        {
            Stream = stream;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            Stream.Observe()
                 .ObserveOn(NewThreadScheduler.Default)
                 .Subscribe(
                             (dashboardData) =>
                             {
                             if (dashboardData != null)
                             {
                                 try
                                 {
                                     var dictionary = new Dictionary<string, string>();
                                     foreach (HystrixCommandMetrics commandMetrics in dashboardData.CommandMetrics)
                                     {
                                         var circuitBreaker = HystrixCircuitBreakerFactory.GetInstance(commandMetrics.CommandKey);
                                         var isOpen = circuitBreaker?.IsOpen;
                                         IHystrixCommandOptions commandProperties = commandMetrics.Properties;

                                             HystrixMetricsEventSource.EventLogger.CommandMetrics(
                                                commandKey: commandMetrics.CommandKey.Name,
                                                commandGroup: commandMetrics.CommandGroup.Name,
                                                isCiruitBreakerOpen: isOpen.HasValue && isOpen.Value,
                                                errorPercent: commandMetrics.Healthcounts.ErrorPercentage,
                                                errorCount: commandMetrics.Healthcounts.ErrorCount,
                                                requestCount: commandMetrics.Healthcounts.TotalRequests,
                                                rollingCountBadRequests: commandMetrics.GetRollingCount(HystrixEventType.BAD_REQUEST),
                                                rollingCountCollapsedRequests: commandMetrics.GetRollingCount(HystrixEventType.COLLAPSED),
                                                rollingCountEmit: commandMetrics.GetRollingCount(HystrixEventType.EMIT),
                                                rollingCountExceptionsThrown: commandMetrics.GetRollingCount(HystrixEventType.EXCEPTION_THROWN),
                                                rollingCountFailure: commandMetrics.GetRollingCount(HystrixEventType.FAILURE),
                                                rollingCountFallbackEmit: commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_EMIT),
                                                rollingCountFallbackFailure: commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_FAILURE),
                                                rollingCountFallbackMissing: commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_MISSING),
                                                rollingCountFallbackRejection: commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_REJECTION),
                                                rollingCountFallbackSuccess: commandMetrics.GetRollingCount(HystrixEventType.FALLBACK_SUCCESS),
                                                rollingCountResponsesFromCache: commandMetrics.GetRollingCount(HystrixEventType.RESPONSE_FROM_CACHE),
                                                rollingCountSemaphoreRejected: commandMetrics.GetRollingCount(HystrixEventType.SEMAPHORE_REJECTED),
                                                rollingCountShortCircuited: commandMetrics.GetRollingCount(HystrixEventType.SHORT_CIRCUITED),
                                                rollingCountSuccess: commandMetrics.GetRollingCount(HystrixEventType.SUCCESS),
                                                rollingCountThreadPoolRejected: commandMetrics.GetRollingCount(HystrixEventType.THREAD_POOL_REJECTED),
                                                rollingCountTimeout: commandMetrics.GetRollingCount(HystrixEventType.TIMEOUT),
                                                currentConcurrentExecutionCount: commandMetrics.CurrentConcurrentExecutionCount,
                                                rollingMaxConcurrentExecutionCount: commandMetrics.RollingMaxConcurrentExecutions,
                                                latencyExecute_mean: commandMetrics.ExecutionTimeMean,
                                                latencyTotal_mean: commandMetrics.TotalTimeMean,
                                                propertyValue_circuitBreakerRequestVolumeThreshold: commandProperties.CircuitBreakerRequestVolumeThreshold,
                                                propertyValue_circuitBreakerSleepWindowInMilliseconds: commandProperties.CircuitBreakerSleepWindowInMilliseconds,
                                                propertyValue_circuitBreakerErrorThresholdPercentage: commandProperties.CircuitBreakerErrorThresholdPercentage,
                                                propertyValue_circuitBreakerForceOpen: commandProperties.CircuitBreakerForceOpen,
                                                propertyValue_circuitBreakerForceClosed: commandProperties.CircuitBreakerForceClosed,
                                                propertyValue_circuitBreakerEnabled: commandProperties.CircuitBreakerEnabled,
                                                propertyValue_executionIsolationStrategy: commandProperties.ExecutionIsolationStrategy.ToString(),
                                                propertyValue_executionIsolationThreadTimeoutInMilliseconds: commandProperties.ExecutionTimeoutInMilliseconds,
                                                propertyValue_executionTimeoutInMilliseconds: commandProperties.ExecutionTimeoutInMilliseconds,
                                                propertyValue_executionIsolationThreadInterruptOnTimeout: false,
                                                propertyValue_executionIsolationThreadPoolKeyOverride: commandProperties.ExecutionIsolationThreadPoolKeyOverride,
                                                propertyValue_executionIsolationSemaphoreMaxConcurrentRequests: commandProperties.ExecutionIsolationSemaphoreMaxConcurrentRequests,
                                                propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests: commandProperties.FallbackIsolationSemaphoreMaxConcurrentRequests,
                                                propertyValue_metricsRollingStatisticalWindowInMilliseconds: commandProperties.MetricsRollingStatisticalWindowInMilliseconds,
                                                propertyValue_requestCacheEnabled: commandProperties.RequestCacheEnabled,
                                                propertyValue_requestLogEnabled: commandProperties.RequestLogEnabled,
                                                reportingHosts: 1, // this will get summed across all instances in a cluster
                                                threadPool: commandMetrics.ThreadPoolKey.Name);
                                          }

                                          foreach (HystrixThreadPoolMetrics threadPoolMetrics in dashboardData.ThreadPoolMetrics)
                                          {
                                              //jsonStrings.Add(ToJsonString(threadPoolMetrics));
                                          }

                                          foreach (HystrixCollapserMetrics collapserMetrics in dashboardData.CollapserMetrics)
                                          {

                                              //collapserMetrics.
                                          }

                                      }
                                      catch (Exception)
                                      {
                                      }
                                  }
                              },
                              (error) =>
                              {
                                  //if (SampleSubscription != null)
                                  //{
                                  //    SampleSubscription.Dispose();
                                  //    SampleSubscription = null;
                                  //}
                              },
                              () =>
                              {
                                  //if (SampleSubscription != null)
                                  //{
                                  //    SampleSubscription.Dispose();
                                  //    SampleSubscription = null;
                                  //}
                              });
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
