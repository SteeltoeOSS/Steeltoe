// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulHeartbeatOptionsTest
    {
        [Fact]
        public void Constructor_InitsDefaults()
        {
            ConsulHeartbeatOptions opts = new ConsulHeartbeatOptions();
            Assert.Equal(30, opts.TtlValue);
            Assert.True(opts.Enabled);
            Assert.Equal("s", opts.TtlUnit);
            Assert.Equal(2.0 / 3.0, opts.IntervalRatio);
            Assert.Equal("30s", opts.Ttl);
        }

        [Theory]
        [InlineData(30, "s", 2.0 / 3.0, 20000)]
        [InlineData(30, "s", 1.0 / 3.0, 10000)]
        [InlineData(10, "m", 0.1, 60000)]
        [InlineData(1, "h", 0.1, 360000)]
        [InlineData(2, "s", 2.0 / 3.0, 1000)]
        [InlineData(1, "s", 2.0 / 3.0, 0)]
        [InlineData(0, "s", 2.0 / 3.0, -1000)]
        public void ComputeHeartbeatIntervalWorks(int ttl, string unit, double ratio, int expected)
        {
            ConsulHeartbeatOptions opts = new ConsulHeartbeatOptions();
            opts.TtlValue = ttl;
            opts.TtlUnit = unit;
            opts.IntervalRatio = ratio;

            var period = opts.ComputeHearbeatInterval();
            Assert.Equal(TimeSpan.FromMilliseconds(expected), period);
        }
    }
}
