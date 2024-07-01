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
        Assert.Equal("foobar", app.Name);
        Assert.NotNull(app.Instances);
        Assert.Empty(app.Instances);
        Assert.Null(app.GetInstance("bar"));
    }

    [Fact]
    public void InstancesConstructor_InitializedCorrectly()
    {
        var infos = new List<InstanceInfo>
        {
            new InstanceInfoBuilder().WithId("1").Build(),
            new InstanceInfoBuilder().WithId("2").Build(),
            new InstanceInfoBuilder().WithId("2").Build() // Note duplicate
        };

        var app = new ApplicationInfo("foobar", infos);

        Assert.Equal("foobar", app.Name);
        Assert.NotNull(app.Instances);
        Assert.Equal(2, app.Instances.Count);
        Assert.NotNull(app.GetInstance("1"));
        Assert.NotNull(app.GetInstance("2"));
    }

    [Fact]
    public void Add_Adds()
    {
        var app = new ApplicationInfo("foobar");
        InstanceInfo instance = new InstanceInfoBuilder().WithId("1").Build();

        app.Add(instance);

        Assert.NotNull(app.GetInstance("1"));
        Assert.Equal(instance, app.GetInstance("1"));
        Assert.NotNull(app.Instances);
        Assert.Single(app.Instances);
    }

    [Fact]
    public void Add_Add_Updates()
    {
        var app = new ApplicationInfo("foobar");
        InstanceInfo instance = new InstanceInfoBuilder().WithId("1").WithStatus(InstanceStatus.Down).Build();

        app.Add(instance);

        Assert.NotNull(app.GetInstance("1"));
        Assert.Equal(InstanceStatus.Down, app.GetInstance("1")?.Status);

        InstanceInfo info2 = new InstanceInfoBuilder().WithId("1").WithStatus(InstanceStatus.Up).Build();
        app.Add(info2);

        Assert.Single(app.Instances);
        Assert.NotNull(app.GetInstance("1"));
        Assert.Equal(InstanceStatus.Up, app.GetInstance("1")?.Status);
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
                { "@class", "java.util.Collections$EmptyMap" }
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

        ApplicationInfo? app = ApplicationInfo.FromJson(application);

        Assert.NotNull(app);
        Assert.Equal("myApp", app.Name);
        Assert.NotNull(app.Instances);
        Assert.Single(app.Instances);
        Assert.NotNull(app.GetInstance("InstanceId"));
        InstanceInfo? instance = app.GetInstance("InstanceId");

        Assert.NotNull(instance);
        Assert.Equal("InstanceId", instance.InstanceId);
        Assert.Equal("myApp", instance.AppName);
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
}
