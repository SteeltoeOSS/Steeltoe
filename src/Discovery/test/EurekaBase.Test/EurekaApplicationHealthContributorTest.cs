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

using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaApplicationHealthContributorTest
    {
        [Fact]
        public void GetApplicationsFromConfig_ReturnsExpected()
        {
            EurekaApplicationsHealthContributor contrib = new EurekaApplicationsHealthContributor();
            EurekaClientConfig config = new EurekaClientConfig();
            var apps = contrib.GetApplicationsFromConfig(config);
            Assert.Null(apps);
            config = new EurekaClientConfig()
            {
                HealthMonitoredApps = "foo,bar, boo "
            };

            apps = contrib.GetApplicationsFromConfig(config);
            Assert.NotEmpty(apps);
            Assert.Equal(3, apps.Count);
            Assert.Contains("foo", apps);
            Assert.Contains("bar", apps);
            Assert.Contains("boo", apps);
        }

        [Fact]
        public void AddApplicationHealthStatus_AddsExpected()
        {
            EurekaApplicationsHealthContributor contrib = new EurekaApplicationsHealthContributor();
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1", Status = InstanceStatus.UP });
            app1.Add(new InstanceInfo() { InstanceId = "id2", Status = InstanceStatus.UP });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1", Status = InstanceStatus.DOWN });
            app2.Add(new InstanceInfo() { InstanceId = "id2", Status = InstanceStatus.STARTING });

            HealthCheckResult result = new HealthCheckResult();
            contrib.AddApplicationHealthStatus("app1", null, result);
            Assert.Equal(HealthStatus.DOWN, result.Status);
            Assert.Equal("No instances found", result.Details["app1"]);

            result = new HealthCheckResult();
            contrib.AddApplicationHealthStatus("foobar", app1, result);
            Assert.Equal(HealthStatus.DOWN, result.Status);
            Assert.Equal("No instances found", result.Details["foobar"]);

            result = new HealthCheckResult();
            result.Status = HealthStatus.UP;
            contrib.AddApplicationHealthStatus("app1", app1, result);
            Assert.Equal(HealthStatus.UP, result.Status);
            Assert.Equal("2 instances with UP status", result.Details["app1"]);

            result = new HealthCheckResult();
            result.Status = HealthStatus.UP;
            contrib.AddApplicationHealthStatus("app2", app2, result);
            Assert.Equal(HealthStatus.DOWN, result.Status);
            Assert.Equal("0 instances with UP status", result.Details["app2"]);
        }
    }
}
