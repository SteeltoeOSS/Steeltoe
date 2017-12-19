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


using Microsoft.Extensions.Options;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Test;

using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream.Test
{
    public class HystrixMetricsStreamPublisherTest: HystrixTestBase
    {
        [Fact]
        public void Constructor_SetsupStream()
        {
            var stream = HystrixDashboardStream.GetInstance();
            var options = new OptionsWrapper<HystrixMetricsStreamOptions>()
            {
                Value = new HystrixMetricsStreamOptions()
            };
            var publisher = new HystrixMetricsStreamPublisher(options, stream);
            Assert.NotNull(publisher.SampleSubscription);
            publisher.SampleSubscription.Dispose();
        }

    }

    class OptionsWrapper<T> : IOptions<T> where T: class, new()
    {
        public T Value { get; set; }
    }
}
