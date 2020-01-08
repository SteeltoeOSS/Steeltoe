// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.Discovery;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{
    public class HystrixMetricsStreamPublisher : IDisposable
    {
        protected const string SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE = "spring.cloud.hystrix.stream";

        protected IDisposable sampleSubscription;
        protected IObservable<List<string>> observable;
        protected IDiscoveryClient discoveryClient;
        protected ILogger logger;
        protected HystrixMetricsStreamOptions options;

        protected internal IDisposable SampleSubscription { get => sampleSubscription; set => sampleSubscription = value; }

        public HystrixMetricsStreamPublisher(IOptions<HystrixMetricsStreamOptions> options, HystrixDashboardStream stream, ILogger logger = null, IDiscoveryClient discoveryClient = null)
        {
            this.discoveryClient = discoveryClient;
            this.logger = logger;
            this.options = options.Value;

            observable = stream.Observe().Map((data) =>
            {
                return Serialize.ToJsonList(data, this.discoveryClient);
            });

            StartMetricsPublishing();
        }

        public void StartMetricsPublishing()
        {
            logger?.LogInformation("Hystrix Metrics starting");

            SampleSubscription = observable
            .ObserveOn(NewThreadScheduler.Default)
            .Subscribe(
                (jsonList) =>
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
                (error) =>
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
}
