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

using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class DefaultHealthAggregatorTest : BaseTest
    {
        [Fact]
        public void Aggregate_NullContributorList_ReturnsExpectedHealth()
        {
            var agg = new DefaultHealthAggregator();
            var result = agg.Aggregate(null);
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.UNKNOWN, result.Status);
            Assert.NotNull(result.Details);
        }

        [Fact]
        public void Aggregate_SingleContributor_ReturnsExpectedHealth()
        {
            var contribs = new List<IHealthContributor>()
            {
                new UpContributor()
            };
            var agg = new DefaultHealthAggregator();
            var result = agg.Aggregate(contribs);
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.NotNull(result.Details);
        }

        [Fact]
        public void Aggregate_MultipleContributor_ReturnsExpectedHealth()
        {
            var contribs = new List<IHealthContributor>()
            {
                new DownContributor(),
                new UpContributor(),
                new OutOfSserviceContributor(),
                new UnknownContributor()
            };
            var agg = new DefaultHealthAggregator();
            var result = agg.Aggregate(contribs);
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.DOWN, result.Status);
            Assert.NotNull(result.Details);
        }

        [Fact]
        public void Aggregate_DuplicateContributor_ReturnsExpectedHealth()
        {
            var contribs = new List<IHealthContributor>()
            {
                new UpContributor(),
                new UpContributor()
            };
            var agg = new DefaultHealthAggregator();
            var result = agg.Aggregate(contribs);
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.Contains("Up-1", result.Details.Keys);
        }

        [Fact]
        public void Aggregate_MultipleContributor_OrderDoesntMatter_ReturnsExpectedHealth()
        {
            var contribs = new List<IHealthContributor>()
            {
                new UpContributor(),
                new OutOfSserviceContributor(),
                new UnknownContributor()
            };
            var agg = new DefaultHealthAggregator();
            var result = agg.Aggregate(contribs);
            Assert.NotNull(result);
            Assert.Equal(HealthStatus.OUT_OF_SERVICE, result.Status);
            Assert.NotNull(result.Details);
        }
    }
}
