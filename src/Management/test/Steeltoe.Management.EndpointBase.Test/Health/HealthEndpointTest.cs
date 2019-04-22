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

using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Health.Test
{
    public class HealthEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsOptionsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new HealthEndpoint(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new HealthEndpoint(new HealthOptions(), null, null));
            Assert.Throws<ArgumentNullException>(() => new HealthEndpoint(new HealthOptions(), new DefaultHealthAggregator(), null));
        }

        [Fact]
        public void Invoke_NoContributors_ReturnsExpectedHealth()
        {
            var opts = new HealthOptions();
            var contributors = new List<IHealthContributor>();
            var agg = new DefaultHealthAggregator();
            var ep = new HealthEndpoint(opts, agg, contributors, GetLogger<HealthEndpoint>());

            var health = ep.Invoke();
            Assert.NotNull(health);
            Assert.Equal(HealthStatus.UNKNOWN, health.Status);
        }

        [Fact]
        public void Invoke_CallsAllContributors()
        {
            var opts = new HealthOptions();
            var contributors = new List<IHealthContributor>() { new TestContrib("h1"), new TestContrib("h2"), new TestContrib("h3") };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contributors);

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                TestContrib tc = (TestContrib)contrib;
                Assert.True(tc.Called);
            }
        }

        [Fact]
        public void Invoke_HandlesExceptions_ReturnsExpectedHealth()
        {
            var opts = new HealthOptions();
            var contributors = new List<IHealthContributor>() { new TestContrib("h1"), new TestContrib("h2", true), new TestContrib("h3") };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contributors);

            var info = ep.Invoke();

            foreach (var contrib in contributors)
            {
                TestContrib tc = (TestContrib)contrib;
                if (tc.Throws)
                {
                    Assert.False(tc.Called);
                }
                else
                {
                    Assert.True(tc.Called);
                }
            }

            Assert.Equal(HealthStatus.UP, info.Status);
        }

        [Fact]
        public void GetStatusCode_ReturnsExpected()
        {
            var opts = new HealthOptions();
            var contribs = new List<IHealthContributor>() { new DiskSpaceContributor() };
            var ep = new HealthEndpoint(opts, new DefaultHealthAggregator(), contribs);

            Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.DOWN }));
            Assert.Equal(503, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.OUT_OF_SERVICE }));
            Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.UP }));
            Assert.Equal(200, ep.GetStatusCode(new HealthCheckResult { Status = HealthStatus.UNKNOWN }));
        }
    }
}
