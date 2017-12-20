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

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CloudFoundry.Connector.Hystrix;
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
            where T : class, new()
        {
            public T Value { get; set; }
        }
    }
}