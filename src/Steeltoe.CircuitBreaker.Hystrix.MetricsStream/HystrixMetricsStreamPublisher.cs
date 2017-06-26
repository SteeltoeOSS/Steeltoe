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
using RabbitMQ.Client;
using System.Reactive.Observable.Aliases;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Pivotal.Discovery.Client;
using System.Text;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{

    public class HystrixMetricsStreamPublisher
    {
        IObservable<List<string>> observable;
        internal ConnectionFactory factory;
        internal IDisposable sampleSubscription;
        IDiscoveryClient discoveryClient;
        ILogger<HystrixMetricsStreamPublisher> logger;

        private const string SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE = "spring.cloud.hystrix.stream";

        public HystrixMetricsStreamPublisher(HystrixDashboardStream stream, HystrixConnectionFactory factory, ILogger<HystrixMetricsStreamPublisher> logger = null, IDiscoveryClient discoveryClient = null)
        {
            this.discoveryClient = discoveryClient;
            this.logger = logger;

            observable = stream.Observe().Map((data) => {
                return Serialize.ToJsonList(data, this.discoveryClient); });

            this.factory = factory.ConnectionFactory as ConnectionFactory;

            StartMetricsPublishing();
        }

        public void StartMetricsPublishing()
        {
            logger?.LogInformation("Hystrix Metrics starting");

            sampleSubscription = observable
            .ObserveOn(NewThreadScheduler.Default)
            .Subscribe(
                (jsonList) =>
                {

                    try
                    {
                        using (var connection = factory.CreateConnection())
                        {
                            using (var channel = connection.CreateModel())
                            {
                                foreach (var sampleDataAsString in jsonList)
                                {
                                    if (!string.IsNullOrEmpty(sampleDataAsString))
                                    {
                                        logger?.LogDebug("Hystrix Metrics: {0}", sampleDataAsString.ToString());

                                        var body = Encoding.UTF8.GetBytes(sampleDataAsString);
                                        var props = channel.CreateBasicProperties();
                                        props.ContentType = "application/json";
                                        channel.BasicPublish(SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE, "", props, body);
                                    }
                                }
                            }
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
                    }

                },
                (error) =>
                {
                    logger?.LogError("Error sending metrics to Hystrix dashboard, metrics streaming disabled: {0}", error);
                    if (sampleSubscription != null)
                    {
                        sampleSubscription.Dispose();
                        sampleSubscription = null;
                    }
                },
                () =>
                {
                    logger?.LogInformation("Hystrix Metrics shutdown");
                    if (sampleSubscription != null)
                    {
                        sampleSubscription.Dispose();
                        sampleSubscription = null;
                    }
                });


        }
    }
}
