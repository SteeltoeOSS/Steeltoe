// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            List<IHealthContributor> contribs = new List<IHealthContributor>()
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
            List<IHealthContributor> contribs = new List<IHealthContributor>()
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
            List<IHealthContributor> contribs = new List<IHealthContributor>()
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
            List<IHealthContributor> contribs = new List<IHealthContributor>()
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
