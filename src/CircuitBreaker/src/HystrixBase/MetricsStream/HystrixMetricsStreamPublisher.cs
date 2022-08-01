// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.Discovery;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream;

public class HystrixMetricsStreamPublisher : IDisposable
{
    protected const string SpringCloudHystrixStreamExchange = "spring.cloud.hystrix.stream";

    protected IObservable<List<string>> observable;
    protected IDiscoveryClient discoveryClient;
    protected ILogger logger;
    protected HystrixMetricsStreamOptions options;

    protected internal IDisposable SampleSubscription { get; set; }

    public HystrixMetricsStreamPublisher(IOptions<HystrixMetricsStreamOptions> options, HystrixDashboardStream stream, ILogger logger = null, IDiscoveryClient discoveryClient = null)
    {
        this.discoveryClient = discoveryClient;
        this.logger = logger;
        this.options = options.Value;

        observable = stream.Observe().Map(data => Serialize.ToJsonList(data, this.discoveryClient));

        StartMetricsPublishing();
    }

    public void StartMetricsPublishing()
    {
        logger?.LogInformation("Hystrix Metrics starting");

        SampleSubscription = observable
            .ObserveOn(NewThreadScheduler.Default)
            .Subscribe(
                jsonList =>
                {
                    try
                    {
                        if (EnsureConnection())
                        {
                            OnNext(jsonList);
                        }
                        else
                        {
                            logger?.LogDebug("Discarding Hystrix Metrics, no connection");
                        }
                    }
                    catch (Exception e)
                    {
                        logger?.LogError("Error sending metrics to Hystrix dashboard, metrics streaming disabled: {0}", e);
                        if (SampleSubscription != null)
                        {
                            SampleSubscription.Dispose();
                            SampleSubscription = null;
                        }

                        Dispose();
                    }
                },
                error =>
                {
                    OnError(error);

                    logger?.LogError("Error sending metrics to Hystrix dashboard, metrics streaming disabled: {0}", error);
                    if (SampleSubscription != null)
                    {
                        SampleSubscription.Dispose();
                        SampleSubscription = null;
                    }

                    Dispose();
                },
                () =>
                {
                    OnComplete();

                    logger?.LogInformation("Hystrix Metrics shutdown");
                    if (SampleSubscription != null)
                    {
                        SampleSubscription.Dispose();
                        SampleSubscription = null;
                    }

                    Dispose();
                });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    protected virtual bool EnsureConnection()
    {
        return true;
    }

    protected virtual void OnNext(List<string> jsonList)
    {
    }

    protected virtual void OnError(Exception error)
    {
    }

    protected virtual void OnComplete()
    {
    }
}
