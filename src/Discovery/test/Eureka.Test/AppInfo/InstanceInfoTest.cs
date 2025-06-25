// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class InstanceInfoTest
{
    [Fact]
    public void Constructor_InitializedWithDefaults()
    {
        var instance = new InstanceInfo("x", "x", "x", "x", new DataCenterInfo(), TimeProvider.System);

        Assert.Null(instance.OverriddenStatus);
        Assert.False(instance.IsSecurePortEnabled);
        Assert.False(instance.IsNonSecurePortEnabled);
        Assert.Null(instance.CountryId);
        Assert.Equal(0, instance.NonSecurePort);
        Assert.Equal(0, instance.SecurePort);
        Assert.Null(instance.Sid);
        Assert.Null(instance.IsCoordinatingDiscoveryServer);
        Assert.Empty(instance.Metadata);
        Assert.False(instance.IsDirty);
        Assert.Equal(instance.LastDirtyTimeUtc, instance.LastUpdatedTimeUtc);
        Assert.Null(instance.Status);
    }

    [Fact]
    public void Constructor_RemovesEmptyMetadataValues()
    {
        var instance = new InstanceInfo("x", "x", "x", "x", new DataCenterInfo(), TimeProvider.System)
        {
            Metadata = new Dictionary<string, string?>
            {
                ["key1"] = null,
                ["key2"] = string.Empty,
                ["key3"] = "value"
            }
        };

        instance.Metadata.Should().ContainSingle();
        instance.Metadata.Should().ContainSingle(pair => pair.Key == "key3" && pair.Value == "value");
    }

    [Fact]
    public void FromJsonInstance_Correct()
    {
        var jsonInstance = new JsonInstanceInfo
        {
            InstanceId = "InstanceId",
            AppName = "AppName",
            AppGroupName = "AppGroupName",
            IPAddress = "IPAddress",
            Sid = "Sid",
            Port = new JsonPortWrapper
            {
                Enabled = true,
                Port = 100
            },
            SecurePort = new JsonPortWrapper
            {
                Enabled = false,
                Port = 100
            },
            HomePageUrl = "HomePageUrl",
            StatusPageUrl = "StatusPageUrl",
            HealthCheckUrl = "HealthCheckUrl",
            SecureHealthCheckUrl = "SecureHealthCheckUrl",
            VipAddress = "VipAddress",
            SecureVipAddress = "SecureVipAddress",
            CountryId = 1,
            DataCenterInfo = new JsonDataCenterInfo
            {
                ClassName = string.Empty,
                Name = "MyOwn"
            },
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
            Metadata = new Dictionary<string, string?>
            {
                ["@class"] = "java.util.Collections$EmptyMap"
            },
            LastUpdatedTimestamp = 1_457_973_741_708,
            LastDirtyTimestamp = 1_457_973_741_708,
            ActionType = ActionType.Added,
            AutoScalingGroupName = "AsgName"
        };

        InstanceInfo? instance = InstanceInfo.FromJson(jsonInstance, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.Equal("InstanceId", instance.InstanceId);
        Assert.Equal("AppName", instance.AppName);
        Assert.Equal("AppGroupName", instance.AppGroupName);
        Assert.Equal("IPAddress", instance.IPAddress);
        Assert.Equal("Sid", instance.Sid);
        Assert.Equal(100, instance.NonSecurePort);
        Assert.True(instance.IsNonSecurePortEnabled);
        Assert.Equal(100, instance.SecurePort);
        Assert.False(instance.IsSecurePortEnabled);
        Assert.Equal("HomePageUrl", instance.HomePageUrl);
        Assert.Equal("StatusPageUrl", instance.StatusPageUrl);
        Assert.Equal("HealthCheckUrl", instance.HealthCheckUrl);
        Assert.Equal("SecureHealthCheckUrl", instance.SecureHealthCheckUrl);
        Assert.Equal("VipAddress", instance.VipAddress);
        Assert.Equal("SecureVipAddress", instance.SecureVipAddress);
        Assert.Equal(1, instance.CountryId);
        Assert.Equal("MyOwn", instance.DataCenterInfo.Name.ToString());
        Assert.Equal("HostName", instance.HostName);
        Assert.Equal(InstanceStatus.Down, instance.Status);
        Assert.Equal(InstanceStatus.OutOfService, instance.OverriddenStatus);
        Assert.NotNull(instance.LeaseInfo);
        Assert.NotNull(instance.LeaseInfo.RenewalInterval);
        Assert.Equal(1, instance.LeaseInfo.RenewalInterval.Value.TotalSeconds);
        Assert.NotNull(instance.LeaseInfo.Duration);
        Assert.Equal(2, instance.LeaseInfo.Duration.Value.TotalSeconds);
        Assert.NotNull(instance.LeaseInfo.RegistrationTimeUtc);
        Assert.Equal(635_935_705_417_080_000L, instance.LeaseInfo.RegistrationTimeUtc.Value.Ticks);
        Assert.NotNull(instance.LeaseInfo.LastRenewalTimeUtc);
        Assert.Equal(635_935_705_417_080_000L, instance.LeaseInfo.LastRenewalTimeUtc.Value.Ticks);
        Assert.NotNull(instance.LeaseInfo.EvictionTimeUtc);
        Assert.Equal(635_935_705_417_080_000L, instance.LeaseInfo.EvictionTimeUtc.Value.Ticks);
        Assert.NotNull(instance.LeaseInfo.ServiceUpTimeUtc);
        Assert.Equal(635_935_705_417_080_000L, instance.LeaseInfo.ServiceUpTimeUtc.Value.Ticks);
        Assert.False(instance.IsCoordinatingDiscoveryServer);
        Assert.NotNull(instance.Metadata);
        Assert.Empty(instance.Metadata);
        Assert.NotNull(instance.LastUpdatedTimeUtc);
        Assert.Equal(635_935_705_417_080_000L, instance.LastUpdatedTimeUtc.Value.Ticks);
        Assert.NotNull(instance.LastDirtyTimeUtc);
        Assert.Equal(635_935_705_417_080_000L, instance.LastDirtyTimeUtc.Value.Ticks);
        Assert.Equal(ActionType.Added, instance.ActionType);
        Assert.Equal("AsgName", instance.AutoScalingGroupName);
    }

    [Fact]
    public void FromJsonInstance_FallsBackToLegacyOverriddenStatus()
    {
        var jsonInstance = new JsonInstanceInfo
        {
            InstanceId = "InstanceId",
            AppName = "AppName",
            AppGroupName = "AppGroupName",
            IPAddress = "IPAddress",
            Sid = "Sid",
            Port = new JsonPortWrapper
            {
                Enabled = true,
                Port = 100
            },
            SecurePort = new JsonPortWrapper
            {
                Enabled = false,
                Port = 100
            },
            HomePageUrl = "HomePageUrl",
            StatusPageUrl = "StatusPageUrl",
            HealthCheckUrl = "HealthCheckUrl",
            SecureHealthCheckUrl = "SecureHealthCheckUrl",
            VipAddress = "VipAddress",
            SecureVipAddress = "SecureVipAddress",
            CountryId = 1,
            DataCenterInfo = new JsonDataCenterInfo
            {
                ClassName = string.Empty,
                Name = "MyOwn"
            },
            HostName = "HostName",
            Status = InstanceStatus.Down,
            OverriddenStatusLegacy = InstanceStatus.OutOfService,
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
            Metadata = new Dictionary<string, string?>
            {
                ["@class"] = "java.util.Collections$EmptyMap"
            },
            LastUpdatedTimestamp = 1_457_973_741_708,
            LastDirtyTimestamp = 1_457_973_741_708,
            ActionType = ActionType.Added,
            AutoScalingGroupName = "AsgName"
        };

        InstanceInfo? instance = InstanceInfo.FromJson(jsonInstance, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.Equal(InstanceStatus.OutOfService, instance.OverriddenStatus);
    }

    [Fact]
    public void FromJsonInstance_NonLegacyOverriddenStatusTakesPrecedence()
    {
        var jsonInstance = new JsonInstanceInfo
        {
            InstanceId = "InstanceId",
            AppName = "AppName",
            AppGroupName = "AppGroupName",
            IPAddress = "IPAddress",
            Sid = "Sid",
            Port = new JsonPortWrapper
            {
                Enabled = true,
                Port = 100
            },
            SecurePort = new JsonPortWrapper
            {
                Enabled = false,
                Port = 100
            },
            HomePageUrl = "HomePageUrl",
            StatusPageUrl = "StatusPageUrl",
            HealthCheckUrl = "HealthCheckUrl",
            SecureHealthCheckUrl = "SecureHealthCheckUrl",
            VipAddress = "VipAddress",
            SecureVipAddress = "SecureVipAddress",
            CountryId = 1,
            DataCenterInfo = new JsonDataCenterInfo
            {
                ClassName = string.Empty,
                Name = "MyOwn"
            },
            HostName = "HostName",
            Status = InstanceStatus.Down,
            OverriddenStatusLegacy = InstanceStatus.Down,
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
            Metadata = new Dictionary<string, string?>
            {
                ["@class"] = "java.util.Collections$EmptyMap"
            },
            LastUpdatedTimestamp = 1_457_973_741_708,
            LastDirtyTimestamp = 1_457_973_741_708,
            ActionType = ActionType.Added,
            AutoScalingGroupName = "AsgName"
        };

        InstanceInfo? instance = InstanceInfo.FromJson(jsonInstance, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.Equal(InstanceStatus.OutOfService, instance.OverriddenStatus);
    }

    [Fact]
    public void FromInstanceConfiguration_DefaultInstanceOptions_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "127.0.0.1",
            HostName = "localhost",
            InstanceId = "foo",
            NonSecurePort = 80,
            IsNonSecurePortEnabled = true,
            AppName = "unknown"
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.Equal(instanceOptions.HostName, instance.HostName);
        Assert.Equal("foo", instance.InstanceId);
        Assert.Equal("UNKNOWN", instance.AppName);
        Assert.Null(instance.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, instance.IPAddress);
        Assert.Null(instance.Sid);
        Assert.Equal(80, instance.NonSecurePort);
        Assert.True(instance.IsNonSecurePortEnabled);
        Assert.Equal(0, instance.SecurePort);
        Assert.False(instance.IsSecurePortEnabled);
        Assert.Equal($"http://{instanceOptions.HostName}:80/", instance.HomePageUrl);
        Assert.Equal($"http://{instanceOptions.HostName}:80/info", instance.StatusPageUrl);
        Assert.Equal($"http://{instanceOptions.HostName}:80/health", instance.HealthCheckUrl);
        Assert.Null(instance.SecureHealthCheckUrl);
        Assert.Null(instance.VipAddress);
        Assert.Null(instance.SecureVipAddress);
        Assert.Null(instance.CountryId);
        Assert.Equal("MyOwn", instance.DataCenterInfo.Name.ToString());
        Assert.Equal(InstanceStatus.Up, instance.Status);
        Assert.Null(instance.OverriddenStatus);
        Assert.NotNull(instance.LeaseInfo);
        Assert.NotNull(instance.LeaseInfo.RenewalInterval);
        Assert.Equal(30, instance.LeaseInfo.RenewalInterval.Value.TotalSeconds);
        Assert.NotNull(instance.LeaseInfo.Duration);
        Assert.Equal(90, instance.LeaseInfo.Duration.Value.TotalSeconds);
        Assert.Null(instance.LeaseInfo.RegistrationTimeUtc);
        Assert.Null(instance.LeaseInfo.LastRenewalTimeUtc);
        Assert.Null(instance.LeaseInfo.EvictionTimeUtc);
        Assert.Null(instance.LeaseInfo.ServiceUpTimeUtc);
        Assert.Null(instance.IsCoordinatingDiscoveryServer);
        Assert.NotNull(instance.Metadata);
        Assert.Empty(instance.Metadata);
        Assert.Equal(instance.LastDirtyTimeUtc, instance.LastUpdatedTimeUtc);
        Assert.Null(instance.ActionType);
        Assert.Null(instance.AutoScalingGroupName);
    }

    [Fact]
    public void FromInstanceConfiguration_URLs_OnlySecurePortEnabled_UsesSecurePort()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "test.domain.com",
            InstanceId = "demo",
            AppName = "my-app",
            IsNonSecurePortEnabled = false,
            IsSecurePortEnabled = true,
            SecurePort = 9090
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.False(instance.IsNonSecurePortEnabled);
        Assert.True(instance.IsSecurePortEnabled);
        Assert.Equal(9090, instance.SecurePort);
        Assert.Equal("https://test.domain.com:9090/", instance.HomePageUrl);
        Assert.Equal("https://test.domain.com:9090/info", instance.StatusPageUrl);
        Assert.Equal("https://test.domain.com:9090/health", instance.HealthCheckUrl);
        Assert.Equal("https://test.domain.com:9090/health", instance.SecureHealthCheckUrl);
    }

    [Fact]
    public void FromInstanceConfiguration_URLs_OnlyNonSecurePortEnabled_UsesNonSecurePort()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "test.domain.com",
            InstanceId = "demo",
            AppName = "my-app",
            IsNonSecurePortEnabled = true,
            NonSecurePort = 8080,
            IsSecurePortEnabled = false
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.True(instance.IsNonSecurePortEnabled);
        Assert.Equal(8080, instance.NonSecurePort);
        Assert.False(instance.IsSecurePortEnabled);
        Assert.Equal("http://test.domain.com:8080/", instance.HomePageUrl);
        Assert.Equal("http://test.domain.com:8080/info", instance.StatusPageUrl);
        Assert.Equal("http://test.domain.com:8080/health", instance.HealthCheckUrl);
        Assert.Null(instance.SecureHealthCheckUrl);
    }

    [Fact]
    public void FromInstanceConfiguration_URLs_BothPortsEnabled_UsesSecurePort()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "test.domain.com",
            InstanceId = "demo",
            AppName = "my-app",
            IsNonSecurePortEnabled = true,
            NonSecurePort = 8080,
            IsSecurePortEnabled = true,
            SecurePort = 9090
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.True(instance.IsNonSecurePortEnabled);
        Assert.Equal(8080, instance.NonSecurePort);
        Assert.True(instance.IsSecurePortEnabled);
        Assert.Equal(9090, instance.SecurePort);
        Assert.Equal("https://test.domain.com:9090/", instance.HomePageUrl);
        Assert.Equal("https://test.domain.com:9090/info", instance.StatusPageUrl);
        Assert.Equal("https://test.domain.com:9090/health", instance.HealthCheckUrl);
        Assert.Equal("https://test.domain.com:9090/health", instance.SecureHealthCheckUrl);
    }

    [Fact]
    public void FromInstanceConfiguration_URLs_NoPortsEnabled_NoURLs()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "test.domain.com",
            InstanceId = "demo",
            AppName = "my-app",
            IsNonSecurePortEnabled = false,
            IsSecurePortEnabled = false
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.False(instance.IsNonSecurePortEnabled);
        Assert.False(instance.IsSecurePortEnabled);
        Assert.Null(instance.HomePageUrl);
        Assert.Null(instance.StatusPageUrl);
        Assert.Null(instance.HealthCheckUrl);
        Assert.Null(instance.SecureHealthCheckUrl);
    }

    [Fact]
    public void FromInstanceConfiguration_ExplicitURLs_SubstitutesEurekaHostname()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "example-host.com",
            InstanceId = "demo",
            AppName = "my-app",
            HomePageUrl = "http://www.${eureka.hostname}/home.html",
            StatusPageUrl = "http://www.${eureka.hostname}/status.html",
            HealthCheckUrl = "http://www.${eureka.hostname}/health.html",
            SecureHealthCheckUrl = "https://www.${eureka.hostname}/health.html"
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);

        Assert.NotNull(instance);
        Assert.Equal("http://www.example-host.com/home.html", instance.HomePageUrl);
        Assert.Equal("http://www.example-host.com/status.html", instance.StatusPageUrl);
        Assert.Equal("http://www.example-host.com/health.html", instance.HealthCheckUrl);
        Assert.Equal("https://www.example-host.com/health.html", instance.SecureHealthCheckUrl);
    }

    [Fact]
    public void ToJsonInstance_DefaultInstanceConfiguration_Correct()
    {
        var instanceOptions = new EurekaInstanceOptions
        {
            IPAddress = "192.168.0.1",
            HostName = "test.domain.com",
            InstanceId = "demo",
            AppName = "my-app"
        };

        InstanceInfo instance = InstanceInfo.FromConfiguration(instanceOptions, TimeProvider.System);
        JsonInstanceInfo jsonInstance = instance.ToJson();

        Assert.Equal(instanceOptions.HostName, jsonInstance.HostName);
        Assert.Equal("demo", jsonInstance.InstanceId);
        Assert.Equal("MY-APP", jsonInstance.AppName);
        Assert.Null(jsonInstance.AppGroupName);
        Assert.Equal(instanceOptions.IPAddress, jsonInstance.IPAddress);
        Assert.Null(jsonInstance.Sid);
        Assert.NotNull(jsonInstance.Port);
        Assert.False(jsonInstance.Port.Enabled);
        Assert.Equal(0, jsonInstance.Port.Port);
        Assert.NotNull(jsonInstance.SecurePort);
        Assert.False(jsonInstance.SecurePort.Enabled);
        Assert.Equal(0, jsonInstance.SecurePort.Port);
        Assert.Null(jsonInstance.HomePageUrl);
        Assert.Null(jsonInstance.StatusPageUrl);
        Assert.Null(jsonInstance.HealthCheckUrl);
        Assert.Null(jsonInstance.SecureHealthCheckUrl);
        Assert.Null(jsonInstance.VipAddress);
        Assert.Null(jsonInstance.SecureVipAddress);
        Assert.Null(jsonInstance.CountryId);
        Assert.NotNull(jsonInstance.DataCenterInfo);
        Assert.Equal("MyOwn", jsonInstance.DataCenterInfo.Name);
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", jsonInstance.DataCenterInfo.ClassName);
        Assert.Equal(InstanceStatus.Up, jsonInstance.Status);
        Assert.Null(jsonInstance.OverriddenStatus);
        Assert.Equal(InstanceStatus.Unknown, jsonInstance.OverriddenStatusLegacy);
        Assert.NotNull(jsonInstance.LeaseInfo);
        Assert.Equal(30, jsonInstance.LeaseInfo.RenewalIntervalInSeconds);
        Assert.Equal(90, jsonInstance.LeaseInfo.DurationInSeconds);
        Assert.Null(jsonInstance.LeaseInfo.RegistrationTimestamp);
        Assert.Null(jsonInstance.LeaseInfo.LastRenewalTimestamp);
        Assert.Null(jsonInstance.LeaseInfo.LastRenewalTimestampLegacy);
        Assert.Null(jsonInstance.LeaseInfo.EvictionTimestamp);
        Assert.Null(jsonInstance.LeaseInfo.ServiceUpTimestamp);
        Assert.Null(jsonInstance.IsCoordinatingDiscoveryServer);
        Assert.NotNull(jsonInstance.Metadata);
        Assert.Single(jsonInstance.Metadata);
        Assert.True(jsonInstance.Metadata.ContainsKey("@class"));
        Assert.Equal("java.util.Collections$EmptyMap", jsonInstance.Metadata["@class"]);
        Assert.Equal(jsonInstance.LastDirtyTimestamp, jsonInstance.LastUpdatedTimestamp);
        Assert.Null(jsonInstance.ActionType);
        Assert.Null(jsonInstance.AutoScalingGroupName);
    }

    [Fact]
    public void Equals_Equals()
    {
        var info1 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);
        var info2 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        Assert.True(info1.Equals(info2));
    }

    [Fact]
    public void Equals_NotEqual()
    {
        var info1 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);
        var info2 = new InstanceInfo("foobar2", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        Assert.False(info1.Equals(info2));
    }

    [Fact]
    public void Equals_NotEqual_DiffTypes()
    {
        var info1 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.False(info1.Equals(this));
    }
}
