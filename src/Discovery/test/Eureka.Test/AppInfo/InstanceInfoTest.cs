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
            var info = new InstanceInfo();
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
        public void FromInstanceConfig_DefaultInstanceConfig_Correct()
        {
            var config = new EurekaInstanceConfig();
            var info = InstanceInfo.FromInstanceConfig(config);
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
            var config = new EurekaInstanceConfig
            {
                SecurePortEnabled = true,
                IsNonSecurePortEnabled = false
            };
            var info = InstanceInfo.FromInstanceConfig(config);
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
        public void Equals_Equals()
        {
            var info1 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };
            var info2 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };

            Assert.True(info1.Equals(info2));
        }

        [Fact]
        public void Equals_NotEqual()
        {
            var info1 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };

            var info2 = new InstanceInfo()
            {
                InstanceId = "foobar2"
            };
            Assert.False(info1.Equals(info2));
        }

        [Fact]
        public void Equals_NotEqual_DiffTypes()
        {
            var info1 = new InstanceInfo()
            {
                InstanceId = "foobar"
            };

            Assert.False(info1.Equals(this));
        }
    }
}
