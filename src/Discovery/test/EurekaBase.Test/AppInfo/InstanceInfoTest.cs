// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System.Collections.Generic;

using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test
{
    public class InstanceInfoTest : AbstractBaseTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            InstanceInfo info = new InstanceInfo();
            Assert.Equal(InstanceStatus.UNKNOWN, info.OverriddenStatus);
            Assert.False(info.IsSecurePortEnabled);
            Assert.True(info.IsUnsecurePortEnabled);
            Assert.Equal(1, info.CountryId);
            Assert.Equal(7001, info.Port);
            Assert.Equal(7002, info.SecurePort);
            Assert.Equal("na", info.Sid);
            Assert.False(info.IsCoordinatingDiscoveryServer);
            Assert.NotNull(info.Metadata);
            Assert.False(info.IsDirty);
            Assert.Equal(info.LastDirtyTimestamp, info.LastUpdatedTimestamp);
            Assert.Equal(InstanceStatus.UP, info.Status);
        }

        [Fact]
        public void FromJsonInstance_Correct()
        {
            JsonInstanceInfo jinfo = new JsonInstanceInfo()
            {
                InstanceId = "InstanceId",
                AppName = "AppName",
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
                LeaseInfo = new JsonLeaseInfo()
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
                Metadata = new Dictionary<string, string>() { { "@class", "java.util.Collections$EmptyMap" } },
                LastUpdatedTimestamp = 1457973741708,
                LastDirtyTimestamp = 1457973741708,
                Actiontype = ActionType.ADDED,
                AsgName = "AsgName"
            };

            InstanceInfo info = InstanceInfo.FromJsonInstance(jinfo);
            Assert.NotNull(info);

            // Verify
            Assert.Equal("InstanceId", info.InstanceId);
            Assert.Equal("AppName", info.AppName);
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

        [Fact]
        public void FromInstanceConfig_DefaultInstanceConfig_Correct()
        {
            EurekaInstanceConfig config = new EurekaInstanceConfig();
            InstanceInfo info = InstanceInfo.FromInstanceConfig(config);
            Assert.NotNull(info);

            // Verify
            Assert.Equal(config.GetHostName(false), info.InstanceId);
            Assert.Equal(EurekaInstanceConfig.Default_Appname.ToUpperInvariant(), info.AppName);
            Assert.Null(info.AppGroupName);
            Assert.Equal(config.IpAddress, info.IpAddr);
            Assert.Equal("na", info.Sid);
            Assert.Equal(80, info.Port);
            Assert.True(info.IsUnsecurePortEnabled);
            Assert.Equal(443, info.SecurePort);
            Assert.False(info.IsSecurePortEnabled);
            Assert.Equal("http://" + config.GetHostName(false) + ":" + 80 + "/", info.HomePageUrl);
            Assert.Equal("http://" + config.GetHostName(false) + ":" + 80 + "/Status", info.StatusPageUrl);
            Assert.Equal("http://" + config.GetHostName(false) + ":" + 80 + "/healthcheck", info.HealthCheckUrl);
            Assert.Null(info.SecureHealthCheckUrl);
            Assert.Equal(config.GetHostName(false) + ":" + 80, info.VipAddress);
            Assert.Equal(config.GetHostName(false) + ":" + 443, info.SecureVipAddress);
            Assert.Equal(1, info.CountryId);
            Assert.Equal("MyOwn", info.DataCenterInfo.Name.ToString());
            Assert.Equal(config.GetHostName(false), info.HostName);
            Assert.Equal(InstanceStatus.STARTING, info.Status);
            Assert.Equal(InstanceStatus.UNKNOWN, info.OverriddenStatus);
            Assert.NotNull(info.LeaseInfo);
            Assert.Equal(30, info.LeaseInfo.RenewalIntervalInSecs);
            Assert.Equal(90, info.LeaseInfo.DurationInSecs);
            Assert.Equal(0, info.LeaseInfo.RegistrationTimestamp);
            Assert.Equal(0, info.LeaseInfo.LastRenewalTimestamp);
            Assert.Equal(0, info.LeaseInfo.LastRenewalTimestampLegacy);
            Assert.Equal(0, info.LeaseInfo.EvictionTimestamp);
            Assert.Equal(0, info.LeaseInfo.ServiceUpTimestamp);
            Assert.False(info.IsCoordinatingDiscoveryServer);
            Assert.NotNull(info.Metadata);
            Assert.Empty(info.Metadata);
            Assert.Equal(info.LastDirtyTimestamp, info.LastUpdatedTimestamp);
            Assert.Equal(ActionType.ADDED, info.Actiontype);
            Assert.Null(info.AsgName);
        }

        [Fact]
        public void FromInstanceConfig_NonSecurePortFalse_SecurePortTrue_Correct()
        {
            EurekaInstanceConfig config = new EurekaInstanceConfig
            {
                SecurePortEnabled = true,
                IsNonSecurePortEnabled = false
            };
            InstanceInfo info = InstanceInfo.FromInstanceConfig(config);
            Assert.NotNull(info);

            // Verify
            Assert.Equal(config.GetHostName(false), info.InstanceId);
            Assert.Equal(EurekaInstanceConfig.Default_Appname.ToUpperInvariant(), info.AppName);
            Assert.Null(info.AppGroupName);
            Assert.Equal(config.IpAddress, info.IpAddr);
            Assert.Equal("na", info.Sid);
            Assert.Equal(80, info.Port);
            Assert.False(info.IsUnsecurePortEnabled);
            Assert.Equal(443, info.SecurePort);
            Assert.True(info.IsSecurePortEnabled);
            Assert.Equal("https://" + config.GetHostName(false) + ":" + 443 + "/", info.HomePageUrl);
            Assert.Equal("https://" + config.GetHostName(false) + ":" + 443 + "/Status", info.StatusPageUrl);
            Assert.Equal("https://" + config.GetHostName(false) + ":" + 443 + "/healthcheck", info.HealthCheckUrl);
            Assert.Null(info.SecureHealthCheckUrl);
            Assert.Equal(config.GetHostName(false) + ":" + 80, info.VipAddress);
            Assert.Equal(config.GetHostName(false) + ":" + 443, info.SecureVipAddress);
            Assert.Equal(1, info.CountryId);
            Assert.Equal("MyOwn", info.DataCenterInfo.Name.ToString());
            Assert.Equal(config.GetHostName(false), info.HostName);
            Assert.Equal(InstanceStatus.STARTING, info.Status);
            Assert.Equal(InstanceStatus.UNKNOWN, info.OverriddenStatus);
            Assert.NotNull(info.LeaseInfo);
            Assert.Equal(30, info.LeaseInfo.RenewalIntervalInSecs);
            Assert.Equal(90, info.LeaseInfo.DurationInSecs);
            Assert.Equal(0, info.LeaseInfo.RegistrationTimestamp);
            Assert.Equal(0, info.LeaseInfo.LastRenewalTimestamp);
            Assert.Equal(0, info.LeaseInfo.LastRenewalTimestampLegacy);
            Assert.Equal(0, info.LeaseInfo.EvictionTimestamp);
            Assert.Equal(0, info.LeaseInfo.ServiceUpTimestamp);
            Assert.False(info.IsCoordinatingDiscoveryServer);
            Assert.NotNull(info.Metadata);
            Assert.Empty(info.Metadata);
            Assert.Equal(info.LastDirtyTimestamp, info.LastUpdatedTimestamp);
            Assert.Equal(ActionType.ADDED, info.Actiontype);
            Assert.Null(info.AsgName);
        }

        [Fact]
        public void ToJsonInstance_DefaultInstanceConfig_Correct()
        {
            EurekaInstanceConfig config = new EurekaInstanceConfig();
            InstanceInfo info = InstanceInfo.FromInstanceConfig(config);
            Assert.NotNull(info);

            JsonInstanceInfo jinfo = info.ToJsonInstance();

            // Verify
            Assert.Equal(config.GetHostName(false), jinfo.InstanceId);
            Assert.Equal(EurekaInstanceConfig.Default_Appname.ToUpperInvariant(), jinfo.AppName);
            Assert.Null(jinfo.AppGroupName);
            Assert.Equal(config.IpAddress, jinfo.IpAddr);
            Assert.Equal("na", jinfo.Sid);
            Assert.NotNull(jinfo.Port);
            Assert.Equal(80, jinfo.Port.Port);
            Assert.True(jinfo.Port.Enabled);
            Assert.NotNull(jinfo.SecurePort);
            Assert.Equal(443, jinfo.SecurePort.Port);
            Assert.False(jinfo.SecurePort.Enabled);
            Assert.Equal("http://" + config.GetHostName(false) + ":" + 80 + "/", jinfo.HomePageUrl);
            Assert.Equal("http://" + config.GetHostName(false) + ":" + 80 + "/Status", jinfo.StatusPageUrl);
            Assert.Equal("http://" + config.GetHostName(false) + ":" + 80 + "/healthcheck", jinfo.HealthCheckUrl);
            Assert.Null(jinfo.SecureHealthCheckUrl);
            Assert.Equal(config.GetHostName(false) + ":" + 80, jinfo.VipAddress);
            Assert.Equal(config.GetHostName(false) + ":" + 443, jinfo.SecureVipAddress);
            Assert.Equal(1, jinfo.CountryId);
            Assert.NotNull(jinfo.DataCenterInfo);
            Assert.Equal("MyOwn", jinfo.DataCenterInfo.Name);
            Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", jinfo.DataCenterInfo.ClassName);
            Assert.Equal(config.GetHostName(false), jinfo.HostName);
            Assert.Equal(InstanceStatus.STARTING, jinfo.Status);
            Assert.Equal(InstanceStatus.UNKNOWN, jinfo.OverriddenStatus);
            Assert.NotNull(jinfo.LeaseInfo);
            Assert.Equal(30, jinfo.LeaseInfo.RenewalIntervalInSecs);
            Assert.Equal(90, jinfo.LeaseInfo.DurationInSecs);
            Assert.Equal(0, jinfo.LeaseInfo.RegistrationTimestamp);
            Assert.Equal(0, jinfo.LeaseInfo.LastRenewalTimestamp);
            Assert.Equal(0, jinfo.LeaseInfo.LastRenewalTimestampLegacy);
            Assert.Equal(0, jinfo.LeaseInfo.EvictionTimestamp);
            Assert.Equal(0, jinfo.LeaseInfo.ServiceUpTimestamp);
            Assert.False(jinfo.IsCoordinatingDiscoveryServer);
            Assert.NotNull(jinfo.Metadata);
            Assert.Single(jinfo.Metadata);
            Assert.True(jinfo.Metadata.ContainsKey("@class"));
            Assert.True(jinfo.Metadata.ContainsValue("java.util.Collections$EmptyMap"));
            Assert.Equal(jinfo.LastDirtyTimestamp, jinfo.LastUpdatedTimestamp);
            Assert.Equal(ActionType.ADDED, jinfo.Actiontype);
            Assert.Null(jinfo.AsgName);
        }

        [Fact]
        public void Equals_Equals()
        {
            InstanceInfo info1 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };
            InstanceInfo info2 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };

            Assert.True(info1.Equals(info2));
        }

        [Fact]
        public void Equals_NotEqual()
        {
            InstanceInfo info1 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };

            InstanceInfo info2 = new InstanceInfo()
            {
                InstanceId = "foobar2"
            };
            Assert.False(info1.Equals(info2));
        }

        [Fact]
        public void Equals_NotEqual_DiffTypes()
        {
            InstanceInfo info1 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };

            Assert.False(info1.Equals(this));
        }
    }
}
