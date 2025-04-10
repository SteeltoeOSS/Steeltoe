// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulHeartbeatOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var options = new ConsulHeartbeatOptions();
        Assert.Equal(30, options.TtlValue);
        Assert.True(options.Enabled);
        Assert.Equal("s", options.TtlUnit);
        Assert.Equal(2.0 / 3.0, options.IntervalRatio);
        Assert.Equal("30s", options.TimeToLive);
    }

    [Theory]
    [InlineData(30, "s", 2.0 / 3.0, 20_000)]
    [InlineData(30, "s", 1.0 / 3.0, 10_000)]
    [InlineData(10, "m", 0.1, 60_000)]
    [InlineData(1, "h", 0.1, 360_000)]
    [InlineData(2, "s", 2.0 / 3.0, 1000)]
    [InlineData(1, "s", 2.0 / 3.0, 0)]
    [InlineData(0, "s", 2.0 / 3.0, -1000)]
    public void ComputeHeartbeatIntervalWorks(int ttl, string unit, double ratio, int expected)
    {
        var options = new ConsulHeartbeatOptions
        {
            TtlValue = ttl,
            TtlUnit = unit,
            IntervalRatio = ratio
        };

        TimeSpan period = options.ComputeHeartbeatInterval();
        Assert.Equal(TimeSpan.FromMilliseconds(expected), period);
    }
}
