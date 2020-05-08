// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulHeartbeatOptionsTest
    {
        [Fact]
        public void Constructor_InitsDefaults()
        {
            var opts = new ConsulHeartbeatOptions();
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
            var opts = new ConsulHeartbeatOptions();
            opts.TtlValue = ttl;
            opts.TtlUnit = unit;
            opts.IntervalRatio = ratio;

            var period = opts.ComputeHearbeatInterval();
            Assert.Equal(TimeSpan.FromMilliseconds(expected), period);
        }
    }
}
