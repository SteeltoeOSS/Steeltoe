// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class ApplicationInfoTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var app = new ApplicationInfo("foobar");

        app.Name.Should().Be("foobar");
        app.Instances.Should().BeEmpty();
        app.GetInstance("bar").Should().BeNull();
    }

    [Fact]
    public void InstancesConstructor_InitializedCorrectly()
    {
        List<InstanceInfo> infos =
        [
            new InstanceInfoBuilder().WithId("1").Build(),
            new InstanceInfoBuilder().WithId("2").Build(),
            new InstanceInfoBuilder().WithId("2").Build() // Note duplicate
        ];

        var app = new ApplicationInfo("foobar", infos);

        app.Name.Should().Be("foobar");
        app.Instances.Should().HaveCount(2);
        app.GetInstance("1").Should().NotBeNull();
        app.GetInstance("2").Should().NotBeNull();
    }

    [Fact]
    public void Add_Adds()
    {
        var app = new ApplicationInfo("foobar");
        InstanceInfo instance = new InstanceInfoBuilder().WithId("1").Build();

        app.Add(instance);

        app.GetInstance("1").Should().Be(instance);
        app.Instances.Should().ContainSingle();
    }

    [Fact]
    public void Add_Add_Updates()
    {
        var app = new ApplicationInfo("foobar");
        InstanceInfo info1 = new InstanceInfoBuilder().WithId("1").WithStatus(InstanceStatus.Down).Build();
        app.Add(info1);

        InstanceInfo? instance1 = app.GetInstance("1");

        instance1.Should().NotBeNull();
        instance1.Status.Should().Be(InstanceStatus.Down);

        InstanceInfo info2 = new InstanceInfoBuilder().WithId("1").WithStatus(InstanceStatus.Up).Build();
        app.Add(info2);

        app.Instances.Should().ContainSingle();

        instance1 = app.GetInstance("1");

        instance1.Should().NotBeNull();
        instance1.Status.Should().Be(InstanceStatus.Up);
    }

    [Fact]
    public void FromJsonApplication_Correct()
    {
        var instanceInfo = new JsonInstanceInfo
        {
            InstanceId = "InstanceId",
            AppName = "myApp",
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

        var application = new JsonApplication
        {
            Name = "myApp",
            Instances = [instanceInfo]
        };

        ApplicationInfo? app = ApplicationInfo.FromJson(application, TimeProvider.System);

        app.Should().NotBeNull();
        app.Name.Should().Be("myApp");
        app.Instances.Should().ContainSingle();

        InstanceInfo? instance = app.GetInstance("InstanceId");

        instance.Should().NotBeNull();
        instance.InstanceId.Should().Be("InstanceId");
        instance.AppName.Should().Be("myApp");
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
}
