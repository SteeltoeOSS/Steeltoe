// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.Connector.Hystrix;
using Steeltoe.Discovery;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{
    public class RabbitMetricsStreamPublisher : HystrixMetricsStreamPublisher
    {
        protected internal ConnectionFactory Factory { get; set; }

        protected internal IConnection Connection { get; set; }

        protected internal IModel Channel { get; set; }

        public RabbitMetricsStreamPublisher(IOptions<HystrixMetricsStreamOptions> options, HystrixDashboardStream stream, HystrixConnectionFactory factory, ILogger<RabbitMetricsStreamPublisher> logger = null, IDiscoveryClient discoveryClient = null)
            : base(options, stream, logger, discoveryClient)
        {
            Factory = factory.ConnectionFactory as ConnectionFactory;
            var sslOption = Factory.Ssl;
            if (sslOption != null && sslOption.Enabled)
            {
                logger?.LogInformation("Hystrix Metrics using TLS");
                sslOption.Version = SslProtocols.Tls12 | SslProtocols.Tls13;
                if (!this.options.Validate_Certificates)
                {
                    logger?.LogInformation("Hystrix Metrics disabling certificate validation");
                    sslOption.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors |
                        SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable;
                }
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
                Connection = Factory.CreateConnection();
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
                        logger?.LogDebug("Hystrix Metrics: {0}", sampleDataAsString);

                        var body = Encoding.UTF8.GetBytes(sampleDataAsString);
                        var props = Channel.CreateBasicProperties();
                        props.ContentType = "application/json";
                        Channel.BasicPublish(SPRING_CLOUD_HYSTRIX_STREAM_EXCHANGE, string.Empty, props, body);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
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
