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
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Net.Security;
using Steeltoe.Common.Discovery;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using Microsoft.Extensions.Options;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{

    public class RabbitMetricsStreamPublisher : HystrixMetricsStreamPublisher
    {
        internal ConnectionFactory factory;
        internal IConnection connection;
        internal IModel channel;

        public RabbitMetricsStreamPublisher(IOptions<HystrixMetricsStreamOptions> options, HystrixDashboardStream stream, HystrixConnectionFactory factory, ILogger<RabbitMetricsStreamPublisher> logger = null, IDiscoveryClient discoveryClient = null)
            : base(options, stream, logger, discoveryClient)
        {

            this.factory = factory.ConnectionFactory as ConnectionFactory;
            SslOption sslOption = this.factory.Ssl;
            if (sslOption != null && sslOption.Enabled && !this.options.Validate_Certificates)
            {
                logger?.LogInformation("Hystrix Metrics disabling certificate validation");
                sslOption.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors |
                    SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable;
            }

            StartMetricsPublishing();
        }

        protected override bool EnsureConnection()
        {
            if (connection != null)
            {
                return true;
            }

            try
            {
                connection = this.factory.CreateConnection();
                channel = connection.CreateModel();
                logger?.LogInformation("Hystrix Metrics connected!");
                return true;

            }
            catch (Exception e)
            {
                logger?.LogError("Error creating connection/channel, metrics streaming disabled: {0}", e);
                return false;
            }
        }

        protected override void OnNext(List<string> jsonList)
        {
            if (channel != null)
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

        protected override void Dispose()
        {
            if (channel != null)
            {
                channel.Dispose();
                channel = null;
            }
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
           
        }
    }
}
