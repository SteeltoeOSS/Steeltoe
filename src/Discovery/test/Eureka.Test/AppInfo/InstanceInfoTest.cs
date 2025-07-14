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

        instance.OverriddenStatus.Should().BeNull();
        instance.IsSecurePortEnabled.Should().BeFalse();
        instance.IsNonSecurePortEnabled.Should().BeFalse();
        instance.CountryId.Should().BeNull();
        instance.NonSecurePort.Should().Be(0);
        instance.SecurePort.Should().Be(0);
        instance.Sid.Should().BeNull();
        instance.IsCoordinatingDiscoveryServer.Should().BeNull();
        instance.Metadata.Should().BeEmpty();
        instance.IsDirty.Should().BeFalse();
        instance.LastUpdatedTimeUtc.Should().Be(instance.LastDirtyTimeUtc);
        instance.Status.Should().BeNull();
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
        instance.Metadata.Should().ContainKey("key3").WhoseValue.Should().Be("value");
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

        instance.Should().NotBeNull();
        instance.InstanceId.Should().Be("InstanceId");
        instance.AppName.Should().Be("AppName");
        instance.AppGroupName.Should().Be("AppGroupName");
        instance.IPAddress.Should().Be("IPAddress");
        instance.Sid.Should().Be("Sid");
        instance.NonSecurePort.Should().Be(100);
        instance.IsNonSecurePortEnabled.Should().BeTrue();
        instance.SecurePort.Should().Be(100);
        instance.IsSecurePortEnabled.Should().BeFalse();
        instance.HomePageUrl.Should().Be("HomePageUrl");
        instance.StatusPageUrl.Should().Be("StatusPageUrl");
        instance.HealthCheckUrl.Should().Be("HealthCheckUrl");
        instance.SecureHealthCheckUrl.Should().Be("SecureHealthCheckUrl");
        instance.VipAddress.Should().Be("VipAddress");
        instance.SecureVipAddress.Should().Be("SecureVipAddress");
        instance.CountryId.Should().Be(1);
        instance.DataCenterInfo.Name.ToString().Should().Be("MyOwn");
        instance.HostName.Should().Be("HostName");
        instance.Status.Should().Be(InstanceStatus.Down);
        instance.OverriddenStatus.Should().Be(InstanceStatus.OutOfService);
        instance.LeaseInfo.Should().NotBeNull();
        instance.LeaseInfo.RenewalInterval.Should().NotBeNull();
        instance.LeaseInfo.RenewalInterval.Value.TotalSeconds.Should().Be(1);
        instance.LeaseInfo.Duration.Should().NotBeNull();
        instance.LeaseInfo.Duration.Value.TotalSeconds.Should().Be(2);
        instance.LeaseInfo.RegistrationTimeUtc.Should().NotBeNull();
        instance.LeaseInfo.RegistrationTimeUtc.Value.Ticks.Should().Be(635_935_705_417_080_000L);
        instance.LeaseInfo.LastRenewalTimeUtc.Should().NotBeNull();
        instance.LeaseInfo.LastRenewalTimeUtc.Value.Ticks.Should().Be(635_935_705_417_080_000L);
        instance.LeaseInfo.EvictionTimeUtc.Should().NotBeNull();
        instance.LeaseInfo.EvictionTimeUtc.Value.Ticks.Should().Be(635_935_705_417_080_000L);
        instance.LeaseInfo.ServiceUpTimeUtc.Should().NotBeNull();
        instance.LeaseInfo.ServiceUpTimeUtc.Value.Ticks.Should().Be(635_935_705_417_080_000L);
        instance.IsCoordinatingDiscoveryServer.Should().BeFalse();
        instance.Metadata.Should().BeEmpty();
        instance.LastUpdatedTimeUtc.Should().NotBeNull();
        instance.LastUpdatedTimeUtc.Value.Ticks.Should().Be(635_935_705_417_080_000L);
        instance.LastDirtyTimeUtc.Should().NotBeNull();
        instance.LastDirtyTimeUtc.Value.Ticks.Should().Be(635_935_705_417_080_000L);
        instance.ActionType.Should().Be(ActionType.Added);
        instance.AutoScalingGroupName.Should().Be("AsgName");
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

        instance.Should().NotBeNull();
        instance.OverriddenStatus.Should().Be(InstanceStatus.OutOfService);
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

        instance.Should().NotBeNull();
        instance.OverriddenStatus.Should().Be(InstanceStatus.OutOfService);
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

        instance.HostName.Should().Be(instanceOptions.HostName);
        instance.InstanceId.Should().Be("foo");
        instance.AppName.Should().Be("UNKNOWN");
        instance.AppGroupName.Should().BeNull();
        instance.IPAddress.Should().Be(instanceOptions.IPAddress);
        instance.Sid.Should().BeNull();
        instance.NonSecurePort.Should().Be(80);
        instance.IsNonSecurePortEnabled.Should().BeTrue();
        instance.SecurePort.Should().Be(0);
        instance.IsSecurePortEnabled.Should().BeFalse();
        instance.HomePageUrl.Should().Be($"http://{instanceOptions.HostName}:80/");
        instance.StatusPageUrl.Should().Be($"http://{instanceOptions.HostName}:80/info");
        instance.HealthCheckUrl.Should().Be($"http://{instanceOptions.HostName}:80/health");
        instance.SecureHealthCheckUrl.Should().BeNull();
        instance.VipAddress.Should().BeNull();
        instance.SecureVipAddress.Should().BeNull();
        instance.CountryId.Should().BeNull();
        instance.DataCenterInfo.Name.ToString().Should().Be("MyOwn");
        instance.Status.Should().Be(InstanceStatus.Up);
        instance.OverriddenStatus.Should().BeNull();
        instance.LeaseInfo.Should().NotBeNull();
        instance.LeaseInfo.RenewalInterval.Should().NotBeNull();
        instance.LeaseInfo.RenewalInterval.Value.TotalSeconds.Should().Be(30);
        instance.LeaseInfo.Duration.Should().NotBeNull();
        instance.LeaseInfo.Duration.Value.TotalSeconds.Should().Be(90);
        instance.LeaseInfo.RegistrationTimeUtc.Should().BeNull();
        instance.LeaseInfo.LastRenewalTimeUtc.Should().BeNull();
        instance.LeaseInfo.EvictionTimeUtc.Should().BeNull();
        instance.LeaseInfo.ServiceUpTimeUtc.Should().BeNull();
        instance.IsCoordinatingDiscoveryServer.Should().BeNull();
        instance.Metadata.Should().BeEmpty();
        instance.LastUpdatedTimeUtc.Should().Be(instance.LastDirtyTimeUtc);
        instance.ActionType.Should().BeNull();
        instance.AutoScalingGroupName.Should().BeNull();
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

        instance.IsNonSecurePortEnabled.Should().BeFalse();
        instance.IsSecurePortEnabled.Should().BeTrue();
        instance.SecurePort.Should().Be(9090);
        instance.HomePageUrl.Should().Be("https://test.domain.com:9090/");
        instance.StatusPageUrl.Should().Be("https://test.domain.com:9090/info");
        instance.HealthCheckUrl.Should().Be("https://test.domain.com:9090/health");
        instance.SecureHealthCheckUrl.Should().Be("https://test.domain.com:9090/health");
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

        instance.IsNonSecurePortEnabled.Should().BeTrue();
        instance.NonSecurePort.Should().Be(8080);
        instance.IsSecurePortEnabled.Should().BeFalse();
        instance.HomePageUrl.Should().Be("http://test.domain.com:8080/");
        instance.StatusPageUrl.Should().Be("http://test.domain.com:8080/info");
        instance.HealthCheckUrl.Should().Be("http://test.domain.com:8080/health");
        instance.SecureHealthCheckUrl.Should().BeNull();
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

        instance.IsNonSecurePortEnabled.Should().BeTrue();
        instance.NonSecurePort.Should().Be(8080);
        instance.IsSecurePortEnabled.Should().BeTrue();
        instance.SecurePort.Should().Be(9090);
        instance.HomePageUrl.Should().Be("https://test.domain.com:9090/");
        instance.StatusPageUrl.Should().Be("https://test.domain.com:9090/info");
        instance.HealthCheckUrl.Should().Be("https://test.domain.com:9090/health");
        instance.SecureHealthCheckUrl.Should().Be("https://test.domain.com:9090/health");
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

        instance.IsNonSecurePortEnabled.Should().BeFalse();
        instance.IsSecurePortEnabled.Should().BeFalse();
        instance.HomePageUrl.Should().BeNull();
        instance.StatusPageUrl.Should().BeNull();
        instance.HealthCheckUrl.Should().BeNull();
        instance.SecureHealthCheckUrl.Should().BeNull();
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

        instance.HomePageUrl.Should().Be("http://www.example-host.com/home.html");
        instance.StatusPageUrl.Should().Be("http://www.example-host.com/status.html");
        instance.HealthCheckUrl.Should().Be("http://www.example-host.com/health.html");
        instance.SecureHealthCheckUrl.Should().Be("https://www.example-host.com/health.html");
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

        jsonInstance.HostName.Should().Be(instanceOptions.HostName);
        jsonInstance.InstanceId.Should().Be("demo");
        jsonInstance.AppName.Should().Be("MY-APP");
        jsonInstance.AppGroupName.Should().BeNull();
        jsonInstance.IPAddress.Should().Be(instanceOptions.IPAddress);
        jsonInstance.Sid.Should().BeNull();
        jsonInstance.Port.Should().NotBeNull();
        jsonInstance.Port.Enabled.Should().BeFalse();
        jsonInstance.Port.Port.Should().Be(0);
        jsonInstance.SecurePort.Should().NotBeNull();
        jsonInstance.SecurePort.Enabled.Should().BeFalse();
        jsonInstance.SecurePort.Port.Should().Be(0);
        jsonInstance.HomePageUrl.Should().BeNull();
        jsonInstance.StatusPageUrl.Should().BeNull();
        jsonInstance.HealthCheckUrl.Should().BeNull();
        jsonInstance.SecureHealthCheckUrl.Should().BeNull();
        jsonInstance.VipAddress.Should().BeNull();
        jsonInstance.SecureVipAddress.Should().BeNull();
        jsonInstance.CountryId.Should().BeNull();
        jsonInstance.DataCenterInfo.Should().NotBeNull();
        jsonInstance.DataCenterInfo.Name.Should().Be("MyOwn");
        jsonInstance.DataCenterInfo.ClassName.Should().Be("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo");
        jsonInstance.Status.Should().Be(InstanceStatus.Up);
        jsonInstance.OverriddenStatus.Should().BeNull();
        jsonInstance.OverriddenStatusLegacy.Should().Be(InstanceStatus.Unknown);
        jsonInstance.LeaseInfo.Should().NotBeNull();
        jsonInstance.LeaseInfo.RenewalIntervalInSeconds.Should().Be(30);
        jsonInstance.LeaseInfo.DurationInSeconds.Should().Be(90);
        jsonInstance.LeaseInfo.RegistrationTimestamp.Should().BeNull();
        jsonInstance.LeaseInfo.LastRenewalTimestamp.Should().BeNull();
        jsonInstance.LeaseInfo.LastRenewalTimestampLegacy.Should().BeNull();
        jsonInstance.LeaseInfo.EvictionTimestamp.Should().BeNull();
        jsonInstance.LeaseInfo.ServiceUpTimestamp.Should().BeNull();
        jsonInstance.IsCoordinatingDiscoveryServer.Should().BeNull();
        jsonInstance.Metadata.Should().ContainSingle();
        jsonInstance.Metadata.Should().ContainKey("@class").WhoseValue.Should().Be("java.util.Collections$EmptyMap");
        jsonInstance.LastUpdatedTimestamp.Should().Be(jsonInstance.LastDirtyTimestamp);
        jsonInstance.ActionType.Should().BeNull();
        jsonInstance.AutoScalingGroupName.Should().BeNull();
    }

    [Fact]
    public void Equals_Equals()
    {
        var info1 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);
        var info2 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        info1.Equals(info2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NotEqual()
    {
        var info1 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);
        var info2 = new InstanceInfo("foobar2", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        info1.Equals(info2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NotEqual_DiffTypes()
    {
        var info1 = new InstanceInfo("foobar", "app", "host", "127.0.0.1", new DataCenterInfo(), TimeProvider.System);

        // ReSharper disable once SuspiciousTypeConversion.Global
        info1.Equals(this).Should().BeFalse();
    }
}
