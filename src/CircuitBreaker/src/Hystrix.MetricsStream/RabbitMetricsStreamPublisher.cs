// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.Connector.Hystrix;
using Steeltoe.Discovery;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream;

public class RabbitMetricsStreamPublisher : HystrixMetricsStreamPublisher
{
    protected internal ConnectionFactory Factory { get; set; }

    protected internal IConnection Connection { get; set; }

    protected internal IModel Channel { get; set; }

    public RabbitMetricsStreamPublisher(IOptions<HystrixMetricsStreamOptions> options, HystrixDashboardStream stream, HystrixConnectionFactory factory,
        ILogger<RabbitMetricsStreamPublisher> logger = null, IDiscoveryClient discoveryClient = null)
        : base(options, stream, logger, discoveryClient)
    {
        Factory = factory.ConnectionFactory as ConnectionFactory;
        SslOption sslOption = Factory.Ssl;

        if (sslOption != null && sslOption.Enabled)
        {
            logger?.LogInformation("Hystrix Metrics using TLS");
            sslOption.Version = SslProtocols.Tls12 | SslProtocols.Tls13;

            if (!this.options.ValidateCertificates)
            {
                logger?.LogInformation("Hystrix Metrics disabling certificate validation");

                sslOption.AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch |
                    SslPolicyErrors.RemoteCertificateNotAvailable;
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
            logger?.LogError(e, "Error creating connection/channel, metrics streaming disabled.");
            return false;
        }
    }

    protected override void OnNext(List<string> jsonList)
    {
        if (Channel != null)
        {
            foreach (string sampleDataAsString in jsonList)
            {
                if (!string.IsNullOrEmpty(sampleDataAsString))
                {
                    logger?.LogDebug("Hystrix Metrics: {data}", sampleDataAsString);

                    byte[] body = Encoding.UTF8.GetBytes(sampleDataAsString);
                    IBasicProperties props = Channel.CreateBasicProperties();
                    props.ContentType = "application/json";
                    Channel.BasicPublish(SpringCloudHystrixStreamExchange, string.Empty, props, body);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Channel?.Dispose();
            Channel = null;

            Connection?.Dispose();
            Connection = null;
        }

        base.Dispose(disposing);
    }
}
