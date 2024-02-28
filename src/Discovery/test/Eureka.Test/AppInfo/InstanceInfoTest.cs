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
                RenewalIntervalInSeconds = 1,
                DurationInSeconds = 2,
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
        Assert.Equal(1, info.LeaseInfo.RenewalInterval.TotalSeconds);
        Assert.Equal(2, info.LeaseInfo.Duration.TotalSeconds);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.RegistrationTimeUtc.Ticks);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.LastRenewalTimeUtc.Ticks);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.EvictionTimeUtc.Ticks);
        Assert.Equal(635_935_705_417_080_000L, info.LeaseInfo.ServiceUpTimeUtc.Ticks);
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
        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(instance);

        // Verify
        Assert.Equal(instanceOptions.ResolveHostName(false), instance.HostName);
        Assert.Equal($"{instance.HostName}:unknown:80", instance.InstanceId);
        Assert.Equal(EurekaInstanceOptions.DefaultAppName.ToUpperInvariant(), instance.AppName);
        Assert.Null(instance.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, instance.IPAddress);
        Assert.Equal("na", instance.Sid);
        Assert.Equal(80, instance.Port);
        Assert.True(instance.IsInsecurePortEnabled);
        Assert.Equal(443, instance.SecurePort);
        Assert.False(instance.IsSecurePortEnabled);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/", instance.HomePageUrl);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/info", instance.StatusPageUrl);
        Assert.Equal($"http://{instanceOptions.ResolveHostName(false)}:80/health", instance.HealthCheckUrl);
        Assert.Null(instance.SecureHealthCheckUrl);
        Assert.Null(instance.VipAddress);
        Assert.Null(instance.SecureVipAddress);
        Assert.Equal(1, instance.CountryId);
        Assert.Equal("MyOwn", instance.DataCenterInfo.Name.ToString());
        Assert.Equal(instanceOptions.ResolveHostName(false), instance.HostName);
        Assert.Equal(InstanceStatus.Up, instance.Status);
        Assert.Equal(InstanceStatus.Unknown, instance.OverriddenStatus);
        Assert.NotNull(instance.LeaseInfo);
        Assert.Equal(30, instance.LeaseInfo.RenewalInterval.TotalSeconds);
        Assert.Equal(90, instance.LeaseInfo.Duration.TotalSeconds);
        Assert.Equal(0, instance.LeaseInfo.RegistrationTimeUtc.Ticks);
        Assert.Equal(0, instance.LeaseInfo.LastRenewalTimeUtc.Ticks);
        Assert.Equal(0, instance.LeaseInfo.EvictionTimeUtc.Ticks);
        Assert.Equal(0, instance.LeaseInfo.ServiceUpTimeUtc.Ticks);
        Assert.False(instance.IsCoordinatingDiscoveryServer);
        Assert.NotNull(instance.Metadata);
        Assert.Empty(instance.Metadata);
        Assert.Equal(instance.LastDirtyTimestamp, instance.LastUpdatedTimestamp);
        Assert.Equal(ActionType.Added, instance.ActionType);
        Assert.Null(instance.AsgName);
    }

    [Fact]
    public void FromInstanceConfiguration_NonSecurePortFalse_SecurePortTrue_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IsSecurePortEnabled = true,
            IsNonSecurePortEnabled = false
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(instance);

        // Verify
        Assert.Equal(instanceOptions.ResolveHostName(false), instance.HostName);
        Assert.Equal($"{instance.HostName}:unknown:80", instance.InstanceId);
        Assert.Equal(EurekaInstanceOptions.DefaultAppName.ToUpperInvariant(), instance.AppName);
        Assert.Null(instance.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, instance.IPAddress);
        Assert.Equal("na", instance.Sid);
        Assert.Equal(80, instance.Port);
        Assert.False(instance.IsInsecurePortEnabled);
        Assert.Equal(443, instance.SecurePort);
        Assert.True(instance.IsSecurePortEnabled);
        Assert.Equal($"https://{instanceOptions.ResolveHostName(false)}:443/", instance.HomePageUrl);
        Assert.Equal($"https://{instanceOptions.ResolveHostName(false)}:443/info", instance.StatusPageUrl);
        Assert.Equal($"https://{instanceOptions.ResolveHostName(false)}:443/health", instance.HealthCheckUrl);
        Assert.Null(instance.SecureHealthCheckUrl);
        Assert.Null(instance.VipAddress);
        Assert.Null(instance.SecureVipAddress);
        Assert.Equal(1, instance.CountryId);
        Assert.Equal("MyOwn", instance.DataCenterInfo.Name.ToString());
        Assert.Equal(instanceOptions.ResolveHostName(false), instance.HostName);
        Assert.Equal(InstanceStatus.Up, instance.Status);
        Assert.Equal(InstanceStatus.Unknown, instance.OverriddenStatus);
        Assert.NotNull(instance.LeaseInfo);
        Assert.Equal(30, instance.LeaseInfo.RenewalInterval.TotalSeconds);
        Assert.Equal(90, instance.LeaseInfo.Duration.TotalSeconds);
        Assert.Equal(0, instance.LeaseInfo.RegistrationTimeUtc.Ticks);
        Assert.Equal(0, instance.LeaseInfo.LastRenewalTimeUtc.Ticks);
        Assert.Equal(0, instance.LeaseInfo.EvictionTimeUtc.Ticks);
        Assert.Equal(0, instance.LeaseInfo.ServiceUpTimeUtc.Ticks);
        Assert.False(instance.IsCoordinatingDiscoveryServer);
        Assert.NotNull(instance.Metadata);
        Assert.Empty(instance.Metadata);
        Assert.Equal(instance.LastDirtyTimestamp, instance.LastUpdatedTimestamp);
        Assert.Equal(ActionType.Added, instance.ActionType);
        Assert.Null(instance.AsgName);
    }

    [Fact]
    public void ToJsonInstance_DefaultInstanceConfiguration_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions();
        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions);
        Assert.NotNull(instance);

        JsonInstanceInfo instanceInfo = instance.ToJsonInstance();

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
        Assert.Equal(30, instanceInfo.LeaseInfo.RenewalIntervalInSeconds);
        Assert.Equal(90, instanceInfo.LeaseInfo.DurationInSeconds);
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
