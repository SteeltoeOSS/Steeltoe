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
using System.Threading.Tasks;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Pivotal.Discovery.Client;
using System.Text;
using Steeltoe.CloudFoundry.Connector.Hystrix;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{
    public class HystrixMetricsStreamPublisher
    {
        IObservable<string> observable;
        ConnectionFactory factory;
        IDisposable sampleSubscription;
        IDiscoveryClient discoveryClient;
        private const string SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE = "spring.cloud.hystrix.stream";

        public HystrixMetricsStreamPublisher(HystrixDashboardStream stream, HystrixConnectionFactory factory, IDiscoveryClient discoveryClient = null)
        {
            this.discoveryClient = discoveryClient;

            observable = stream.Observe().Map((data) => {
                return Serialize.ToJsonString(data, this.discoveryClient); });

            this.factory = factory.ConnectionFactory as ConnectionFactory;

            Task.Factory.StartNew(() => { StartMetricsPublishing(); }, TaskCreationOptions.LongRunning);
        }

        public void StartMetricsPublishing()
        {
            sampleSubscription = observable
            .Subscribe(
                (sampleDataAsString) =>
                {
                    if (sampleDataAsString != null)
                    {
                        try
                        {
                            using (var connection = factory.CreateConnection())
                            {
                                using (var channel = connection.CreateModel())
                                {
                                    var body = Encoding.UTF8.GetBytes(sampleDataAsString);
                                    channel.BasicPublish(SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE, "", null, body);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            if (sampleSubscription != null)
                            {
                                sampleSubscription.Dispose();
                                sampleSubscription = null;
                            }
                        }
                    }
                },
                (error) =>
                {
                    if (sampleSubscription != null)
                    {
                        sampleSubscription.Dispose();
                        sampleSubscription = null;
                    }
                },
                () =>
                {
                    if (sampleSubscription != null)
                    {
                        sampleSubscription.Dispose();
                        sampleSubscription = null;
                    }
                });


        }
    }
}
