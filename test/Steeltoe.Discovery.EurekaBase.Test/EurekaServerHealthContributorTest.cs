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
using Steeltoe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaServerHealthContributorTest
    {
        [Fact]
        public void MakeHealthStatus_ReturnsExpected()
        {
            EurekaServerHealthContributor contrib = new EurekaServerHealthContributor();
            Assert.Equal(HealthStatus.DOWN, contrib.MakeHealthStatus(InstanceStatus.DOWN));
            Assert.Equal(HealthStatus.UP, contrib.MakeHealthStatus(InstanceStatus.UP));
            Assert.Equal(HealthStatus.UNKNOWN, contrib.MakeHealthStatus(InstanceStatus.STARTING));
            Assert.Equal(HealthStatus.UNKNOWN, contrib.MakeHealthStatus(InstanceStatus.UNKNOWN));
            Assert.Equal(HealthStatus.OUT_OF_SERVICE, contrib.MakeHealthStatus(InstanceStatus.OUT_OF_SERVICE));
        }

        [Fact]
        public void AddApplications_AddsExpected()
        {
            EurekaServerHealthContributor contrib = new EurekaServerHealthContributor();
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1" });
            app2.Add(new InstanceInfo() { InstanceId = "id2" });

            var apps = new Applications(new List<Application>() { app1, app2 });
            HealthCheckResult result = new HealthCheckResult();
            contrib.AddApplications(apps, result);
            var details = result.Details;
            Assert.Contains("applications", details.Keys);
            var appsDict = details["applications"] as Dictionary<string, int>;
            Assert.Contains("app1", appsDict.Keys);
            Assert.Contains("app2", appsDict.Keys);
            Assert.Equal(2, appsDict.Keys.Count);
            var count1 = appsDict["app1"];
            Assert.Equal(2, count1);
            var count2 = appsDict["app2"];
            Assert.Equal(2, count2);
        }

        [Fact]
        public void AddFetchStatus_AddsExpected()
        {
            EurekaServerHealthContributor contrib = new EurekaServerHealthContributor();
            HealthCheckResult results = new HealthCheckResult();
            contrib.AddFetchStatus(null, results, 0);
            Assert.Contains("fetchStatus", results.Details.Keys);
            Assert.Equal("UNKNOWN", results.Details["fetchStatus"]);

            results = new HealthCheckResult();
            EurekaClientConfig config = new EurekaClientConfig()
            {
                ShouldFetchRegistry = true
            };

            contrib.AddFetchStatus(config, results, 0);
            Assert.Contains("fetch", results.Details.Keys);
            Assert.Contains("not yet successfully connected", (string)results.Details["fetch"]);
            Assert.Contains("fetchTime", results.Details.Keys);
            Assert.Contains("Unknown", (string)results.Details["fetchTime"]);
            Assert.Contains("fetchStatus", results.Details.Keys);
            Assert.Equal("UP", results.Details["fetchStatus"]);

            results = new HealthCheckResult();
            long ticks = DateTime.UtcNow.Ticks - (TimeSpan.TicksPerSecond * config.RegistryFetchIntervalSeconds * 10);
            DateTime dateTime = new DateTime(ticks);
            contrib.AddFetchStatus(config, results, ticks);
            Assert.Contains("fetch", results.Details.Keys);
            Assert.Contains("reporting failures", (string)results.Details["fetch"]);
            Assert.Contains("fetchTime", results.Details.Keys);
            Assert.Equal(dateTime.ToString("s"), (string)results.Details["fetchTime"]);
            Assert.Contains("fetchFailures", results.Details.Keys);
            Assert.Equal(10, (long)results.Details["fetchFailures"]);
            Assert.Contains("fetchStatus", results.Details.Keys);
            Assert.Equal("UP", results.Details["fetchStatus"]);
        }

        [Fact]
        public void AddHeartbeatStatus_AddsExpected()
        {
            EurekaServerHealthContributor contrib = new EurekaServerHealthContributor();
            HealthCheckResult results = new HealthCheckResult();
            contrib.AddHeartbeatStatus(null, null, results, 0);
            Assert.Contains("heartbeatStatus", results.Details.Keys);
            Assert.Equal("UNKNOWN", results.Details["heartbeatStatus"]);

            results = new HealthCheckResult();
            EurekaClientConfig clientconfig = new EurekaClientConfig()
            {
                ShouldRegisterWithEureka = true
            };
            EurekaInstanceConfig instconfig = new EurekaInstanceConfig();

            contrib.AddHeartbeatStatus(clientconfig, instconfig, results, 0);
            Assert.Contains("heartbeat", results.Details.Keys);
            Assert.Contains("not yet successfully connected", (string)results.Details["heartbeat"]);
            Assert.Contains("heartbeatTime", results.Details.Keys);
            Assert.Contains("Unknown", (string)results.Details["heartbeatTime"]);
            Assert.Contains("heartbeatStatus", results.Details.Keys);
            Assert.Equal("UP", results.Details["heartbeatStatus"]);

            results = new HealthCheckResult();
            long ticks = DateTime.UtcNow.Ticks - (TimeSpan.TicksPerSecond * instconfig.LeaseRenewalIntervalInSeconds * 10);
            DateTime dateTime = new DateTime(ticks);
            contrib.AddHeartbeatStatus(clientconfig, instconfig, results, ticks);
            Assert.Contains("heartbeat", results.Details.Keys);
            Assert.Contains("reporting failures", (string)results.Details["heartbeat"]);
            Assert.Contains("heartbeatTime", results.Details.Keys);
            Assert.Equal(dateTime.ToString("s"), (string)results.Details["heartbeatTime"]);
            Assert.Contains("heartbeatFailures", results.Details.Keys);
            Assert.Equal(10, (long)results.Details["heartbeatFailures"]);
            Assert.Contains("heartbeatStatus", results.Details.Keys);
            Assert.Equal("UP", results.Details["heartbeatStatus"]);
        }
    }
}
