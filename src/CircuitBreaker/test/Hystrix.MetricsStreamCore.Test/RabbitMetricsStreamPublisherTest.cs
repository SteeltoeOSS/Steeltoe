// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.Connector.Hystrix;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class RabbitMetricsStreamPublisherTest : HystrixTestBase
    {
        [Fact]
        public void Constructor_SetsupStream()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var factory = new HystrixConnectionFactory(new ConnectionFactory());
            var options = new OptionsWrapper<HystrixMetricsStreamOptions>()
            {
                Value = new HystrixMetricsStreamOptions()
            };
            var publisher = new RabbitMetricsStreamPublisher(options, stream, factory);
            Assert.NotNull(publisher.SampleSubscription);
            Assert.NotNull(publisher.Factory);
            publisher.SampleSubscription.Dispose();
        }

        private class OptionsWrapper<T> : IOptions<T>
            where T : class, new ()
        {
            public T Value { get; set; }
        }
    }
}