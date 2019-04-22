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

using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class MetricsResponseTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var samples = new List<MetricSample>()
            {
                new MetricSample(MetricStatistic.TOTALTIME, 100.00)
            };

            var tags = new List<MetricTag>()
            {
                new MetricTag("tag", new HashSet<string>() { "tagValue" })
            };

            var resp = new MetricsResponse("foo.bar", samples, tags);
            Assert.Equal("foo.bar", resp.Name);
            Assert.Same(samples, resp.Measurements);
            Assert.Same(tags, resp.AvailableTags);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var samples = new List<MetricSample>()
            {
                new MetricSample(MetricStatistic.TOTALTIME, 100.00)
            };

            var tags = new List<MetricTag>()
            {
                new MetricTag("tag", new HashSet<string>() { "tagValue" })
            };

            var resp = new MetricsResponse("foo.bar", samples, tags);
            var result = Serialize(resp);
            Assert.Equal("{\"name\":\"foo.bar\",\"measurements\":[{\"statistic\":\"TOTAL_TIME\",\"value\":100.0}],\"availableTags\":[{\"tag\":\"tag\",\"values\":[\"tagValue\"]}]}", result);
        }
    }
}
