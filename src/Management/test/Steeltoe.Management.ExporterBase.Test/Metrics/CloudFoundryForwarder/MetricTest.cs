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

using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class MetricTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var tags = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };

            var metric = new Metric("foo.bar", MetricType.GAUGE, 1L, "unit", tags, 100);
            Assert.Equal("foo.bar", metric.Name);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("unit", metric.Unit);
            Assert.Equal(100, metric.Value);
            Assert.Same(tags, metric.Tags);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var tags = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };

            var metric = new Metric("foo.bar", MetricType.GAUGE, 1L, "unit", tags, 100);
            var result = Serialize(metric);
            Assert.Equal("{\"name\":\"foo.bar\",\"tags\":{\"foo\":\"bar\"},\"timestamp\":1,\"type\":\"gauge\",\"unit\":\"unit\",\"value\":100.0}", result);
        }
    }
}
