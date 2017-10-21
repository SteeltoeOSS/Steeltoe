//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Reactive.Observable.Aliases;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using Microsoft.Extensions.Options;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{

    public class HystrixMetricsStreamPublisher
    {
        protected IObservable<List<string>> observable;
        internal protected IDisposable sampleSubscription;
        protected IDiscoveryClient discoveryClient;
        protected ILogger logger;
        protected HystrixMetricsStreamOptions options;


        protected const string SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE = "spring.cloud.hystrix.stream";

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
        protected virtual void Dispose()
        {

        }

        public virtual void StartMetricsPublishing()
        {
            logger?.LogInformation("Hystrix Metrics starting");

            sampleSubscription = observable
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
                        if (sampleSubscription != null)
                        {
                            sampleSubscription.Dispose();
                            sampleSubscription = null;
                        }
                        Dispose();

                    }

                },
                (error) =>
                {
                    OnError(error);

                    logger?.LogError("Error sending metrics to Hystrix dashboard, metrics streaming disabled: {0}", error);
                    if (sampleSubscription != null)
                    {
                        sampleSubscription.Dispose();
                        sampleSubscription = null;
                    }
                    Dispose();

                },
                () =>
                {
                    OnComplete();

                    logger?.LogInformation("Hystrix Metrics shutdown");
                    if (sampleSubscription != null)
                    {
                        sampleSubscription.Dispose();
                        sampleSubscription = null;
                    }
                    Dispose();

                });


        }
    }
}
