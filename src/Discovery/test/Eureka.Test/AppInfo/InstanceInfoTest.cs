// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class InstanceInfoTest : AbstractBaseTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var info = new InstanceInfo();
        Assert.Equal(InstanceStatus.Unknown, info.OverriddenStatus);
        Assert.False(info.IsSecurePortEnabled);
        Assert.True(info.IsInsecurePortEnabled);
        Assert.Equal(1, info.CountryId);
        Assert.Equal(7001, info.Port);
        Assert.Equal(7002, info.SecurePort);
        Assert.Equal("na", info.Sid);
        Assert.False(info.IsCoordinatingDiscoveryServer);
        Assert.NotNull(info.Metadata);
        Assert.False(info.IsDirty);
        Assert.Equal(info.LastDirtyTimestamp, info.LastUpdatedTimestamp);
        Assert.Equal(InstanceStatus.Up, info.Status);
    }

    [Fact]
    public void FromJsonInstance_Correct()
    {
        var instanceInfo = new JsonInstanceInfo
        {
            InstanceId = "InstanceId",
            AppName = "AppName",
            AppGroupName = "AppGroupName",
            IPAddress = "IPAddress",
            Sid = "Sid",
            Port = JsonPortWrapper.Create(true, 100),
            SecurePort = JsonPortWrapper.Create(false, 100),
            HomePageUrl = "HomePageUrl",
            StatusPageUrl = "StatusPageUrl",
            HealthCheckUrl = "HealthCheckUrl",
            SecureHealthCheckUrl = "SecureHealthCheckUrl",
            VipAddress = "VipAddress",
            SecureVipAddress = "SecureVipAddress",
            CountryId = 1,
            DataCenterInfo = JsonDataCenterInfo.Create(string.Empty, "MyOwn"),
            HostName = "HostName",
            Status = InstanceStatus.Down,
            OverriddenStatus = InstanceStatus.OutOfService,
            LeaseInfo = new JsonLeaseInfo
            {
                RenewalIntervalInSecs = 1,
                DurationInSecs = 2,
                RegistrationTimestamp = 1_457_973_741_708,
                LastRenewalTimestamp = 1_457_973_741_708,
                LastRenewalTimestampLegacy = 1_457_973_741_708,
                EvictionTimestamp = 1_457_973_741_708,
                ServiceUpTimestamp = 1_457_973_741_708
            },
            IsCoordinatingDiscoveryServer = false,
            Metadata = new Dictionary<string, string>
            {
                { "@class", "java.util.Collections$EmptyMap" }
            },
            LastUpdatedTimestamp = 1_457_973_741_708,
            LastDirtyTimestamp = 1_457_973_741_708,
            ActionType = ActionType.Added,
            AsgName = "AsgName"
        };

        var info = InstanceInfo.FromJsonInstance(instanceInfo);
        Assert.NotNull(info);

        // Verify
        Assert.Equal("InstanceId", info.InstanceId);
        Assert.Equal("AppName", info.AppName);
        Assert.Equal("AppGroupName", info.AppGroupName);
        Assert.Equal("IPAddress", info.IPAddress);
        Assert.Equal("Sid", info.Sid);
        Assert.Equal(100, info.Port);
        Assert.True(info.IsInsecurePortEnabled);
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
        Assert.Equal(InstanceStatus.Down, info.Status);
        Assert.Equal(InstanceStatus.OutOfService, info.OverriddenStatus);
        Assert.NotNull(info.LeaseInfo);
        Assert.Equal(1, info.LeaseInfo.RenewalIntervalInSecs);
        Assert.Equal(2, info.LeaseInfo.DurationInSecs);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.RegistrationTimestamp);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.LastRenewalTimestamp);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.LastRenewalTimestampLegacy);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.EvictionTimestamp);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.ServiceUpTimestamp);
        Assert.False(info.IsCoordinatingDiscoveryServer);
        Assert.NotNull(info.Metadata);
        Assert.Empty(info.Metadata);
        Assert.Equal(635_935_705_417_080_000L, info.LastUpdatedTimestamp);
        Assert.Equal(635_935_705_417_080_000L, info.LastDirtyTimestamp);
        Assert.Equal(ActionType.Added, info.ActionType);
        Assert.Equal("AsgName", info.AsgName);
    }

    [Fact]
    public void FromInstanceConfiguration_DefaultInstanceOptions_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions();
        var info = InstanceInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(info);

        // Verify
        Assert.Equal(instanceOptions.ResolveHostName(false), info.HostName);
        Assert.Equal($"{info.HostName}:unknown:80", info.InstanceId);
        Assert.Equal(EurekaInstanceOptions.DefaultAppName.ToUpperInvariant(), info.AppName);
        Assert.Null(info.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, info.IPAddress);
        Assert.Equal("na", info.Sid);
        Assert.Equal(80, info.Port);
        Assert.True(info.IsInsecurePortEnabled);
        Assert.Equal(443, info.SecurePort);
        Assert.False(info.IsSecurePortEnabled);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/", info.HomePageUrl);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/info", info.StatusPageUrl);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/health", info.HealthCheckUrl);
        Assert.Null(info.SecureHealthCheckUrl);
        Assert.Null(info.VipAddress);
        Assert.Null(info.SecureVipAddress);
        Assert.Equal(1, info.CountryId);
        Assert.Equal("MyOwn", info.DataCenterInfo.Name.ToString());
        Assert.Equal(instanceOptions.ResolveHostName(false), info.HostName);
        Assert.Equal(InstanceStatus.Up, info.Status);
        Assert.Equal(InstanceStatus.Unknown, info.OverriddenStatus);
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
        Assert.Equal(ActionType.Added, info.ActionType);
        Assert.Null(info.AsgName);
    }

    [Fact]
    public void FromInstanceConfiguration_NonSecurePortFalse_SecurePortTrue_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsSecurePortEnabled = true,
            IsNonSecurePortEnabled = false
        };

        var info = InstanceInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(info);

        // Verify
        Assert.Equal(instanceOptions.ResolveHostName(false), info.HostName);
        Assert.Equal($"{info.HostName}:unknown:80", info.InstanceId);
        Assert.Equal(EurekaInstanceOptions.DefaultAppName.ToUpperInvariant(), info.AppName);
        Assert.Null(info.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, info.IPAddress);
        Assert.Equal("na", info.Sid);
        Assert.Equal(80, info.Port);
        Assert.False(info.IsInsecurePortEnabled);
        Assert.Equal(443, info.SecurePort);
        Assert.True(info.IsSecurePortEnabled);
        Assert.Equal($"https://{instanceOptions.ResolveHostName(false)}:443/", info.HomePageUrl);
        Assert.Equal($"https://{instanceOptions.ResolveHostName(false)}:443/info", info.StatusPageUrl);
        Assert.Equal($"https://{instanceOptions.ResolveHostName(false)}:443/health", info.HealthCheckUrl);
        Assert.Null(info.SecureHealthCheckUrl);
        Assert.Null(info.VipAddress);
        Assert.Null(info.SecureVipAddress);
        Assert.Equal(1, info.CountryId);
        Assert.Equal("MyOwn", info.DataCenterInfo.Name.ToString());
        Assert.Equal(instanceOptions.ResolveHostName(false), info.HostName);
        Assert.Equal(InstanceStatus.Up, info.Status);
        Assert.Equal(InstanceStatus.Unknown, info.OverriddenStatus);
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
        Assert.Equal(ActionType.Added, info.ActionType);
        Assert.Null(info.AsgName);
    }

    [Fact]
    public void ToJsonInstance_DefaultInstanceConfiguration_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions();
        var info = InstanceInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(info);

        JsonInstanceInfo instanceInfo = info.ToJsonInstance();

        // Verify
        Assert.Equal(instanceOptions.ResolveHostName(false), instanceInfo.HostName);
        Assert.Equal($"{instanceInfo.HostName}:unknown:80", instanceInfo.InstanceId);
        Assert.Equal(EurekaInstanceOptions.DefaultAppName.ToUpperInvariant(), instanceInfo.AppName);
        Assert.Null(instanceInfo.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, instanceInfo.IPAddress);
        Assert.Equal("na", instanceInfo.Sid);
        Assert.NotNull(instanceInfo.Port);
        Assert.Equal(80, instanceInfo.Port.Port);
        Assert.True(instanceInfo.Port.Enabled);
        Assert.NotNull(instanceInfo.SecurePort);
        Assert.Equal(443, instanceInfo.SecurePort.Port);
        Assert.False(instanceInfo.SecurePort.Enabled);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/", instanceInfo.HomePageUrl);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/info", instanceInfo.StatusPageUrl);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/health", instanceInfo.HealthCheckUrl);
        Assert.Null(instanceInfo.SecureHealthCheckUrl);
        Assert.Null(instanceInfo.VipAddress);
        Assert.Null(instanceInfo.SecureVipAddress);
        Assert.Equal(1, instanceInfo.CountryId);
        Assert.NotNull(instanceInfo.DataCenterInfo);
        Assert.Equal("MyOwn", instanceInfo.DataCenterInfo.Name);
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", instanceInfo.DataCenterInfo.ClassName);
        Assert.Equal(instanceOptions.ResolveHostName(false), instanceInfo.HostName);
        Assert.Equal(InstanceStatus.Up, instanceInfo.Status);
        Assert.Equal(InstanceStatus.Unknown, instanceInfo.OverriddenStatus);
        Assert.NotNull(instanceInfo.LeaseInfo);
        Assert.Equal(30, instanceInfo.LeaseInfo.RenewalIntervalInSecs);
        Assert.Equal(90, instanceInfo.LeaseInfo.DurationInSecs);
        Assert.Equal(0, instanceInfo.LeaseInfo.RegistrationTimestamp);
        Assert.Equal(0, instanceInfo.LeaseInfo.LastRenewalTimestamp);
        Assert.Equal(0, instanceInfo.LeaseInfo.LastRenewalTimestampLegacy);
        Assert.Equal(0, instanceInfo.LeaseInfo.EvictionTimestamp);
        Assert.Equal(0, instanceInfo.LeaseInfo.ServiceUpTimestamp);
        Assert.False(instanceInfo.IsCoordinatingDiscoveryServer);
        Assert.NotNull(instanceInfo.Metadata);
        Assert.Single(instanceInfo.Metadata);
        Assert.True(instanceInfo.Metadata.ContainsKey("@class"));
        Assert.Equal("java.util.Collections$EmptyMap", instanceInfo.Metadata["@class"]);
        Assert.Equal(instanceInfo.LastDirtyTimestamp, instanceInfo.LastUpdatedTimestamp);
        Assert.Equal(ActionType.Added, instanceInfo.ActionType);
        Assert.Null(instanceInfo.AsgName);
    }

    [Fact]
    public void Equals_Equals()
    {
        var info1 = new InstanceInfo
        {
            InstanceId = "foobar"
        };

        var info2 = new InstanceInfo
        {
            InstanceId = "foobar"
        };

        Assert.True(info1.Equals(info2));
    }

    [Fact]
    public void Equals_NotEqual()
    {
        var info1 = new InstanceInfo
        {
            InstanceId = "foobar"
        };

        var info2 = new InstanceInfo
        {
            InstanceId = "foobar2"
        };

        Assert.False(info1.Equals(info2));
    }

    [Fact]
    public void Equals_NotEqual_DiffTypes()
    {
        var info1 = new InstanceInfo
        {
            InstanceId = "foobar"
        };

        Assert.False(info1.Equals(this));
    }
}
