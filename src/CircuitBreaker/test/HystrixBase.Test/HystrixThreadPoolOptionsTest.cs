// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

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
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetNeitherCoreNorMaximumSize()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"));

        Assert.Equal(HystrixThreadPoolOptions.Default_CoreSize, properties.CoreSize);
        Assert.Equal(HystrixThreadPoolOptions.Default_MaximumSize, properties.MaximumSize);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetCoreSizeOnly()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 14 });

        Assert.Equal(14, properties.CoreSize);
        Assert.Equal(HystrixThreadPoolOptions.Default_MaximumSize, properties.MaximumSize);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetMaximumSizeOnlyLowerThanDefaultCoreSize()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { MaximumSize = 3 });
        Assert.Equal(HystrixThreadPoolOptions.Default_CoreSize, properties.CoreSize);
        Assert.Equal(3, properties.MaximumSize);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetMaximumSizeOnlyGreaterThanDefaultCoreSize()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { MaximumSize = 21 });
        Assert.Equal(HystrixThreadPoolOptions.Default_CoreSize, properties.CoreSize);
        Assert.Equal(21, properties.MaximumSize);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetCoreSizeLessThanMaximumSize()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 2, MaximumSize = 8 });
        Assert.Equal(2, properties.CoreSize);
        Assert.Equal(8, properties.MaximumSize);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetCoreSizeEqualToMaximumSize()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 7, MaximumSize = 7 });
        Assert.Equal(7, properties.CoreSize);
        Assert.Equal(7, properties.MaximumSize);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSetCoreSizeGreaterThanMaximumSize()
    {
        var properties = new HystrixThreadPoolOptions(HystrixThreadPoolKeyDefault.AsKey("TEST"), new HystrixThreadPoolOptions() { CoreSize = 12, MaximumSize = 8 });
        Assert.Equal(12, properties.CoreSize);
        Assert.Equal(8, properties.MaximumSize);
    }
}