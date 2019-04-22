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
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class MetricSampleTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var sample = new MetricSample(MetricStatistic.TOTAL, 100.00);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);
            Assert.Equal(100.00, sample.Value);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var sample = new MetricSample(MetricStatistic.TOTAL, 100.00);
            var result = Serialize(sample);
            Assert.Equal("{\"statistic\":\"TOTAL\",\"value\":100.0}", result);
        }
    }
}
