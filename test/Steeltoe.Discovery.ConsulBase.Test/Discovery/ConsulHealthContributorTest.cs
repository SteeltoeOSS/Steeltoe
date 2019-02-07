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

using Consul;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Consul.Discovery.Test
{
    public class ConsulHealthContributorTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            var clientMoq = new Mock<IConsulClient>();
            Assert.Throws<ArgumentNullException>(() => new ConsulHealthContributor(null, new ConsulDiscoveryOptions()));
            Assert.Throws<ArgumentNullException>(() => new ConsulHealthContributor(clientMoq.Object, (ConsulDiscoveryOptions)null));
        }

        [Fact]
        public async void GetLeaderStatusAsync_ReturnsExpected()
        {
            var statusResult = Task.FromResult("thestatus");
            var clientMoq = new Mock<IConsulClient>();
            var statusMoq = new Mock<IStatusEndpoint>();
            clientMoq.Setup(c => c.Status).Returns(statusMoq.Object);
            statusMoq.Setup(s => s.Leader(default(CancellationToken))).Returns(statusResult);

            var contrib = new ConsulHealthContributor(clientMoq.Object, new ConsulDiscoveryOptions());
            var result = await contrib.GetLeaderStatusAsync();
            Assert.Equal("thestatus", result);
        }

        [Fact]
        public async void GetCatalogServicesAsync_ReturnsExpected()
        {
            var queryResult = new QueryResult<Dictionary<string, string[]>>()
            {
                Response = new Dictionary<string, string[]>
                {
                    { "foo", new string[] { "I1", "I2" } },
                    { "bar", new string[] { "I1", "I2" } },
                }
            };

            var catResult = Task.FromResult(queryResult);
            var statusResult = Task.FromResult("thestatus");
            var clientMoq = new Mock<IConsulClient>();
            var catMoq = new Mock<ICatalogEndpoint>();
            clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
            catMoq.Setup(c => c.Services(QueryOptions.Default, default(CancellationToken))).Returns(catResult);

            var contrib = new ConsulHealthContributor(clientMoq.Object, new ConsulDiscoveryOptions());
            var result = await contrib.GetCatalogServicesAsync();
            Assert.Equal(2, result.Count);
            Assert.Contains("foo", result.Keys);
            Assert.Contains("bar", result.Keys);
        }

        [Fact]
        public void Health_ReturnsExpected()
        {
            var queryResult = new QueryResult<Dictionary<string, string[]>>()
            {
                Response = new Dictionary<string, string[]>
                {
                    { "foo", new string[] { "I1", "I2" } },
                    { "bar", new string[] { "I1", "I2" } },
                }
            };

            var catResult = Task.FromResult(queryResult);
            var statusResult = Task.FromResult("thestatus");

            var clientMoq = new Mock<IConsulClient>();
            var catMoq = new Mock<ICatalogEndpoint>();
            var statusMoq = new Mock<IStatusEndpoint>();
            clientMoq.Setup(c => c.Status).Returns(statusMoq.Object);
            clientMoq.Setup(c => c.Catalog).Returns(catMoq.Object);
            statusMoq.Setup(s => s.Leader(default(CancellationToken))).Returns(statusResult);
            catMoq.Setup(c => c.Services(QueryOptions.Default, default(CancellationToken))).Returns(catResult);

            var contrib = new ConsulHealthContributor(clientMoq.Object, new ConsulDiscoveryOptions());
            var result = contrib.Health();

            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.Equal(2, result.Details.Count);

            Assert.Contains("leader", result.Details.Keys);
            Assert.Contains("services", result.Details.Keys);
        }
    }
}
