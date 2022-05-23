// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixMetricsStreamPublisherTest : HystrixTestBase
    {
        [Fact]
        public void Constructor_SetsupStream()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var options = new OptionsWrapper<HystrixMetricsStreamOptions>
            {
                Value = new HystrixMetricsStreamOptions()
            };
            var publisher = new HystrixMetricsStreamPublisher(options, stream);
            Assert.NotNull(publisher.SampleSubscription);
            publisher.SampleSubscription.Dispose();
        }

        internal class OptionsWrapper<T> : IOptions<T>
            where T : class, new()
        {
            public T Value { get; set; }
        }
    }
}
