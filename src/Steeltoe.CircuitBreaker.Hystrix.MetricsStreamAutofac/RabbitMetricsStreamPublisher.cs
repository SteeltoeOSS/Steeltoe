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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CloudFoundry.Connector.Hystrix;
using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{
    public class RabbitMetricsStreamPublisher : HystrixMetricsStreamPublisher
    {
        private ConnectionFactory factory;
        private IConnection connection;
        private IModel channel;

        protected internal ConnectionFactory Factory { get => factory; set => factory = value; }

        protected internal IConnection Connection { get => connection; set => connection = value; }

        protected internal IModel Channel { get => channel; set => channel = value; }

        public RabbitMetricsStreamPublisher(IOptions<HystrixMetricsStreamOptions> options, HystrixDashboardStream stream, HystrixConnectionFactory factory, ILogger<RabbitMetricsStreamPublisher> logger = null, IDiscoveryClient discoveryClient = null)
            : base(options, stream, logger, discoveryClient)
        {
            this.Factory = factory.ConnectionFactory as ConnectionFactory;
            SslOption sslOption = this.Factory.Ssl;
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
            if (Connection != null)
            {
                return true;
            }

            try
            {
                Connection = this.Factory.CreateConnection();
                Channel = Connection.CreateModel();
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
            if (Channel != null)
            {
                foreach (var sampleDataAsString in jsonList)
                {
                    if (!string.IsNullOrEmpty(sampleDataAsString))
                    {
                        logger?.LogDebug("Hystrix Metrics: {0}", sampleDataAsString.ToString());

                        var body = Encoding.UTF8.GetBytes(sampleDataAsString);
                        var props = Channel.CreateBasicProperties();
                        props.ContentType = "application/json";
                        Channel.BasicPublish(SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE, string.Empty, props, body);
                    }
                }
            }
        }

        protected override void Dispose()
        {
            if (Channel != null)
            {
                Channel.Dispose();
                Channel = null;
            }

            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }
    }
}
