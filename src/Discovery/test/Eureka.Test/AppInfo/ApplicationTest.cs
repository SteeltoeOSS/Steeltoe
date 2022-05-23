// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test
{
    public class ApplicationTest : AbstractBaseTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            var app = new Application("foobar");
            Assert.Equal("foobar", app.Name);
            Assert.Equal(0, app.Count);
            Assert.NotNull(app.Instances);
            Assert.Equal(0, app.Instances.Count);
            Assert.Null(app.GetInstance("bar"));
        }

        [Fact]
        public void InstancesConstructor_InitializedCorrectly()
        {
            var infos = new List<InstanceInfo>
            {
                new InstanceInfo { InstanceId = "1" },
                new InstanceInfo { InstanceId = "2" },
                new InstanceInfo { InstanceId = "2" } // Note duplicate
            };

            var app = new Application("foobar", infos);

            Assert.Equal("foobar", app.Name);
            Assert.Equal(2, app.Count);
            Assert.NotNull(app.Instances);
            Assert.Equal(2, app.Instances.Count);
            Assert.Equal(2, app.Instances.Count);
            Assert.NotNull(app.GetInstance("1"));
            Assert.NotNull(app.GetInstance("2"));
        }

        [Fact]
        public void Add_Adds()
        {
            var app = new Application("foobar");
            var info = new InstanceInfo
            {
                InstanceId = "1"
            };

            app.Add(info);

            Assert.NotNull(app.GetInstance("1"));
            Assert.True(app.GetInstance("1") == info);
            Assert.NotNull(app.Instances);
            Assert.Equal(1, app.Count);
            Assert.Equal(app.Count, app.Instances.Count);
        }

        [Fact]
        public void Add_Add_Updates()
        {
            var app = new Application("foobar");
            var info = new InstanceInfo
            {
                InstanceId = "1",
                Status = InstanceStatus.DOWN
            };

            app.Add(info);

            Assert.NotNull(app.GetInstance("1"));
            Assert.Equal(InstanceStatus.DOWN, app.GetInstance("1").Status);

            var info2 = new InstanceInfo
            {
                InstanceId = "1",
                Status = InstanceStatus.UP
            };

            app.Add(info2);
            Assert.Equal(1, app.Count);
            Assert.NotNull(app.GetInstance("1"));
            Assert.Equal(InstanceStatus.UP, app.GetInstance("1").Status);
        }

        [Fact]
        public void FromJsonApplication_Correct()
        {
            var jinfo = new JsonInstanceInfo
            {
                InstanceId = "InstanceId",
                AppName = "myApp",
                AppGroupName = "AppGroupName",
                IpAddr = "IpAddr",
                Sid = "Sid",
                Port = new JsonInstanceInfo.JsonPortWrapper(true, 100),
                SecurePort = new JsonInstanceInfo.JsonPortWrapper(false, 100),
                HomePageUrl = "HomePageUrl",
                StatusPageUrl = "StatusPageUrl",
                HealthCheckUrl = "HealthCheckUrl",
                SecureHealthCheckUrl = "SecureHealthCheckUrl",
                VipAddress = "VipAddress",
                SecureVipAddress = "SecureVipAddress",
                CountryId = 1,
                DataCenterInfo = new JsonInstanceInfo.JsonDataCenterInfo(string.Empty, "MyOwn"),
                HostName = "HostName",
                Status = InstanceStatus.DOWN,
                OverriddenStatus = InstanceStatus.OUT_OF_SERVICE,
                LeaseInfo = new JsonLeaseInfo
                {
                    RenewalIntervalInSecs = 1,
                    DurationInSecs = 2,
                    RegistrationTimestamp = 1457973741708,
                    LastRenewalTimestamp = 1457973741708,
                    LastRenewalTimestampLegacy = 1457973741708,
                    EvictionTimestamp = 1457973741708,
                    ServiceUpTimestamp = 1457973741708
                },
                IsCoordinatingDiscoveryServer = false,
                Metadata = new Dictionary<string, string> { { "@class", "java.util.Collections$EmptyMap" } },
                LastUpdatedTimestamp = 1457973741708,
                LastDirtyTimestamp = 1457973741708,
                Actiontype = ActionType.ADDED,
                AsgName = "AsgName"
            };

            var japp = new JsonApplication
            {
                Name = "myApp",
                Instances = new List<JsonInstanceInfo> { jinfo }
            };

            var app = Application.FromJsonApplication(japp);

            // Verify
            Assert.NotNull(app);
            Assert.Equal("myApp", app.Name);
            Assert.NotNull(app.Instances);
            Assert.Equal(1, app.Count);
            Assert.Equal(1, app.Instances.Count);
            Assert.NotNull(app.GetInstance("InstanceId"));
            var info = app.GetInstance("InstanceId");

            Assert.Equal("InstanceId", info.InstanceId);
            Assert.Equal("myApp", info.AppName);
            Assert.Equal("AppGroupName", info.AppGroupName);
            Assert.Equal("IpAddr", info.IpAddr);
            Assert.Equal("Sid", info.Sid);
            Assert.Equal(100, info.Port);
            Assert.True(info.IsUnsecurePortEnabled);
            Assert.Equal(100, info.SecurePort);
            Assert.False(info.IsSecurePortEnabled);
            Assert.Equal("HomePageUrl", info.HomePageUrl);
            Assert.Equal("StatusPageUrl", info.StatusPageUrl);
            Assert.Equal("HealthCheckUrl", info.HealthCheckUrl);
            Assert.Equal("SecureHealthCheckUrl", info.SecureHealthCheckUrl);
            Assert.Equal("VipAddress", info.VipAddress);
            Assert.Equal("SecureVipAddress", info.SecureVipAddress);
            Assert.Equal(1, info.CountryId);
            Assert.Equal("MyOwn", info.DataCenterInfo.Name.ToString());
            Assert.Equal("HostName", info.HostName);
            Assert.Equal(InstanceStatus.DOWN, info.Status);
            Assert.Equal(InstanceStatus.OUT_OF_SERVICE, info.OverriddenStatus);
            Assert.NotNull(info.LeaseInfo);
            Assert.Equal(1, info.LeaseInfo.RenewalIntervalInSecs);
            Assert.Equal(2, info.LeaseInfo.DurationInSecs);
            Assert.Equal(635935705417080000L, info.LeaseInfo.RegistrationTimestamp);
            Assert.Equal(635935705417080000L, info.LeaseInfo.LastRenewalTimestamp);
            Assert.Equal(635935705417080000L, info.LeaseInfo.LastRenewalTimestampLegacy);
            Assert.Equal(635935705417080000L, info.LeaseInfo.EvictionTimestamp);
            Assert.Equal(635935705417080000L, info.LeaseInfo.ServiceUpTimestamp);
            Assert.False(info.IsCoordinatingDiscoveryServer);
            Assert.NotNull(info.Metadata);
            Assert.Empty(info.Metadata);
            Assert.Equal(635935705417080000L, info.LastUpdatedTimestamp);
            Assert.Equal(635935705417080000L, info.LastDirtyTimestamp);
            Assert.Equal(ActionType.ADDED, info.Actiontype);
            Assert.Equal("AsgName", info.AsgName);
        }
    }
}
