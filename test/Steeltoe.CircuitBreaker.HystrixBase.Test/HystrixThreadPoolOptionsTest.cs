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

using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixThreadPoolOptionsTest
    {
        public static HystrixThreadPoolOptions GetUnitTestPropertiesBuilder()
        {
            return new HystrixThreadPoolOptions()
            {
                CoreSize = 10,   // core size of thread pool
                MaximumSize = 15,  // maximum size of thread pool
                KeepAliveTimeMinutes = 1,   // minutes to keep a thread alive (though in practice this doesn't get used as by default we set a fixed size)
                MaxQueueSize = 100,  // size of queue (but we never allow it to grow this big ... this can't be dynamically changed so we use 'queueSizeRejectionThreshold' to artificially limit and reject)
                QueueSizeRejectionThreshold = 10,  // number of items in queue at which point we reject (this can be dyamically changed)
                MetricsRollingStatisticalWindowInMilliseconds = 10000,   // milliseconds for rolling number
                MetricsRollingStatisticalWindowBuckets = 10 // number of buckets in rolling number (10 1-second buckets)
            };
        }

        [Fact]
        public void TestSetNeitherCoreNorMaximumSize()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"));

            Assert.Equal(HystrixThreadPoolOptions.Default_CoreSize, properties.CoreSize);
            Assert.Equal(HystrixThreadPoolOptions.Default_MaximumSize, properties.MaximumSize);
        }

        [Fact]
        public void TestSetCoreSizeOnly()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 14 });

            Assert.Equal(14, properties.CoreSize);
            Assert.Equal(HystrixThreadPoolOptions.Default_MaximumSize, properties.MaximumSize);
        }

        [Fact]
        public void TestSetMaximumSizeOnlyLowerThanDefaultCoreSize()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { MaximumSize = 3 });
            Assert.Equal(HystrixThreadPoolOptions.Default_CoreSize, properties.CoreSize);
            Assert.Equal(3, properties.MaximumSize);
        }

        [Fact]
        public void TestSetMaximumSizeOnlyGreaterThanDefaultCoreSize()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { MaximumSize = 21 });
            Assert.Equal(HystrixThreadPoolOptions.Default_CoreSize, properties.CoreSize);
            Assert.Equal(21, properties.MaximumSize);
        }

        [Fact]
        public void TestSetCoreSizeLessThanMaximumSize()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 2, MaximumSize = 8 });
            Assert.Equal(2, properties.CoreSize);
            Assert.Equal(8, properties.MaximumSize);
        }

        [Fact]
        public void TestSetCoreSizeEqualToMaximumSize()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 7, MaximumSize = 7 });
            Assert.Equal(7, properties.CoreSize);
            Assert.Equal(7, properties.MaximumSize);
        }

        [Fact]
        public void TestSetCoreSizeGreaterThanMaximumSize()
        {
            HystrixThreadPoolOptions properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 12, MaximumSize = 8 });
            Assert.Equal(12, properties.CoreSize);
            Assert.Equal(8, properties.MaximumSize);
        }
    }
}
