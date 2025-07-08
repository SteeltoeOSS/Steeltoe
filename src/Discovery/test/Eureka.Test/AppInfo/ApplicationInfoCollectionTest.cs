// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class ApplicationInfoCollectionTest
{
    [Fact]
    public void ApplicationsConstructor_AddsAppsFromList()
    {
        var app1 = new ApplicationInfo("app1");
        app1.Add(new InstanceInfoBuilder().WithId("id1").Build());
        app1.Add(new InstanceInfoBuilder().WithId("id2").Build());

        var app2 = new ApplicationInfo("app2");

        app2.Add(new InstanceInfoBuilder().WithId("id1").Build());
        app2.Add(new InstanceInfoBuilder().WithId("id2").Build());

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        apps.ApplicationMap.Should().HaveCount(2);
        apps.ApplicationMap.Should().ContainKey("app1".ToUpperInvariant());
        apps.ApplicationMap.Should().ContainKey("app2".ToUpperInvariant());
    }

    [Fact]
    public void Add_AddsTo_ApplicationMap()
    {
        var app1 = new ApplicationInfo("app1");
        app1.Add(new InstanceInfoBuilder().WithId("id1").Build());
        app1.Add(new InstanceInfoBuilder().WithId("id2").Build());

        var app2 = new ApplicationInfo("app2");
        app2.Add(new InstanceInfoBuilder().WithId("id1").Build());
        app2.Add(new InstanceInfoBuilder().WithId("id2").Build());

        ApplicationInfoCollection apps =
        [
            app1,
            app2
        ];

        apps.ApplicationMap.Should().HaveCount(2);
        apps.ApplicationMap.Should().ContainKey("app1".ToUpperInvariant());
        apps.ApplicationMap.Should().ContainKey("app2".ToUpperInvariant());
    }

    [Fact]
    public void Add_ExpandsTo_ApplicationMap()
    {
        var app1 = new ApplicationInfo("app1");
        app1.Add(new InstanceInfoBuilder().WithId("id1").WithVipAddress("vip1a,vip1b").Build());
        app1.Add(new InstanceInfoBuilder().WithId("id2").WithSecureVipAddress("svip2a,svip2b").Build());

        var apps = new ApplicationInfoCollection([app1]);

        apps.GetInstancesByVipAddress("vip1a").Should().ContainSingle();
        apps.GetInstancesByVipAddress("vip1b").Should().ContainSingle();
        apps.GetInstancesBySecureVipAddress("svip2a").Should().ContainSingle();
        apps.GetInstancesBySecureVipAddress("svip2b").Should().ContainSingle();
    }

    [Fact]
    public void Add_UpdatesExisting_ApplicationMap()
    {
        var app1 = new ApplicationInfo("app1");
        app1.Add(new InstanceInfoBuilder().WithId("id1").Build());
        app1.Add(new InstanceInfoBuilder().WithId("id2").Build());

        var app2 = new ApplicationInfo("app2");
        app2.Add(new InstanceInfoBuilder().WithId("id1").Build());
        app2.Add(new InstanceInfoBuilder().WithId("id2").Build());

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var app1Updated = new ApplicationInfo("app1");
        app1Updated.Add(new InstanceInfoBuilder().WithId("id3").Build());
        app1Updated.Add(new InstanceInfoBuilder().WithId("id4").Build());

        apps.Add(app1Updated);

        apps.ApplicationMap.Should().HaveCount(2);
        ApplicationInfo? app = apps.ApplicationMap.Should().ContainKey("app1".ToUpperInvariant()).WhoseValue;
        app.Instances.Should().ContainSingle(info => info.InstanceId == "id3");
        app.Instances.Should().ContainSingle(info => info.InstanceId == "id4");

        apps.ApplicationMap.Should().ContainKey("app2".ToUpperInvariant());
    }

    [Fact]
    public void Add_AddsTo_VirtualHostInstanceMaps()
    {
        var app1 = new ApplicationInfo("app1");
        app1.Add(new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build());
        app1.Add(new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build());

        var app2 = new ApplicationInfo("app2");
        app2.Add(new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build());
        app2.Add(new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build());

        ApplicationInfoCollection apps =
        [
            app1,
            app2
        ];

        apps.VipInstanceMap.Should().HaveCount(2);
        apps.VipInstanceMap.Should().ContainKey("vapp1".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);
        apps.VipInstanceMap.Should().ContainKey("vapp2".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);

        apps.SecureVipInstanceMap.Should().HaveCount(2);
        apps.SecureVipInstanceMap.Should().ContainKey("svapp1".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);
        apps.SecureVipInstanceMap.Should().ContainKey("svapp2".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);
    }

    [Fact]
    public void GetRegisteredApplications_ReturnsExpected()
    {
        var apps = new ApplicationInfoCollection([
            new ApplicationInfo("app1", [
                new InstanceInfoBuilder().WithId("id1").Build(),
                new InstanceInfoBuilder().WithId("id2").Build()
            ]),
            new ApplicationInfo("app2", [
                new InstanceInfoBuilder().WithId("id1").Build(),
                new InstanceInfoBuilder().WithId("id2").Build()
            ])
        ]);

        apps.Should().HaveCount(2);
        apps.Should().ContainSingle(app => app.Name == "app1");
        apps.Should().ContainSingle(app => app.Name == "app2");
    }

    [Fact]
    public void RemoveInstanceFromVip_UpdatesApp_RemovesFromVirtualHostInstanceMaps()
    {
        var apps = new ApplicationInfoCollection([
            new ApplicationInfo("app1", [
                new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
                new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
            ]),
            new ApplicationInfo("app2", [
                new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build(),
                new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build()
            ])
        ]);

        apps.VipInstanceMap.Should().HaveCount(2);
        apps.VipInstanceMap.Should().ContainKey("vapp1".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);
        apps.VipInstanceMap.Should().ContainKey("vapp2".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);

        apps.SecureVipInstanceMap.Should().HaveCount(2);
        apps.SecureVipInstanceMap.Should().ContainKey("svapp1".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);
        apps.SecureVipInstanceMap.Should().ContainKey("svapp2".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);

        apps.RemoveInstanceFromVip(new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build());
        apps.RemoveInstanceFromVip(new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build());

        apps.VipInstanceMap.Should().ContainSingle();
        apps.VipInstanceMap.Should().NotContainKey("vapp1".ToUpperInvariant());
        apps.VipInstanceMap.TryGetValue("vapp1".ToUpperInvariant(), out _).Should().BeFalse();
        apps.VipInstanceMap.Should().ContainKey("vapp2".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);

        apps.SecureVipInstanceMap.Should().ContainSingle();
        apps.SecureVipInstanceMap.Should().NotContainKey("svapp1".ToUpperInvariant());
        apps.SecureVipInstanceMap.TryGetValue("svapp1".ToUpperInvariant(), out _).Should().BeFalse();
        apps.SecureVipInstanceMap.Should().ContainKey("svapp2".ToUpperInvariant()).WhoseValue.Should().HaveCount(2);
    }

    [Fact]
    public void GetRegisteredApplication_ReturnsExpected()
    {
        var apps = new ApplicationInfoCollection([
            new ApplicationInfo("app1", [
                new InstanceInfoBuilder().WithId("id1").Build(),
                new InstanceInfoBuilder().WithId("id2").Build()
            ]),
            new ApplicationInfo("app2", [
                new InstanceInfoBuilder().WithId("id1").Build(),
                new InstanceInfoBuilder().WithId("id2").Build()
            ])
        ]);

        ApplicationInfo? registered = apps.GetRegisteredApplication("app1");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app1");

        registered = apps.GetRegisteredApplication("foobar");

        registered.Should().BeNull();
    }

    [Fact]
    public void GetInstancesBySecureVipAddress_ReturnsExpected()
    {
        var app1 = new ApplicationInfo("app1", [
            new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2", [
            new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build(),
            new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        List<InstanceInfo> result = apps.GetInstancesBySecureVipAddress("svapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesBySecureVipAddress("svapp2");

        result.Should().HaveCount(2);
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().Contain(app2.GetInstance("id2")!);

        result = apps.GetInstancesBySecureVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetInstancesByVipAddress_ReturnsExpected()
    {
        var app1 = new ApplicationInfo("app1", [
            new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2", [
            new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build(),
            new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp2");

        result.Should().HaveCount(2);
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().Contain(app2.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromDelta_EmptyDelta_NoChange()
    {
        var app1 = new ApplicationInfo("app1",
        [
            new InstanceInfoBuilder().WithAppName("app1").WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithAppName("app1").WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2",
        [
            new InstanceInfoBuilder().WithAppName("app2").WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build(),
            new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var delta = new ApplicationInfoCollection();
        apps.UpdateFromDelta(delta);

        ApplicationInfo? registered = apps.GetRegisteredApplication("app1");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app1");
        registered.Instances.Should().HaveCount(2);

        registered = apps.GetRegisteredApplication("app2");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app2");
        registered.Instances.Should().HaveCount(2);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp2");

        result.Should().HaveCount(2);
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().Contain(app2.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromDelta_AddNewAppNewInstance_UpdatesCorrectly()
    {
        var app1 = new ApplicationInfo("app1",
        [
            new InstanceInfoBuilder().WithAppName("app1").WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithAppName("app1").WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2",
        [
            new InstanceInfoBuilder().WithAppName("app2").WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build(),
            new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var app3 = new ApplicationInfo("app3",
        [
            new InstanceInfoBuilder().WithAppName("app3").WithId("id1").WithVipAddress("vapp3").WithSecureVipAddress("svapp3").WithActionType(ActionType.Added)
                .Build()
        ]);

        var delta = new ApplicationInfoCollection([app3]);

        apps.UpdateFromDelta(delta);

        ApplicationInfo? registered = apps.GetRegisteredApplication("app1");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app1");
        registered.Instances.Should().HaveCount(2);

        registered = apps.GetRegisteredApplication("app2");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app2");
        registered.Instances.Should().HaveCount(2);

        registered = apps.GetRegisteredApplication("app3");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app3");
        registered.Instances.Should().ContainSingle();

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp2");

        result.Should().HaveCount(2);
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().Contain(app2.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp3");

        result.Should().ContainSingle();
        result.Should().Contain(app3.GetInstance("id1")!);

        result = apps.GetInstancesByVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromDelta_ExistingAppWithAddNewInstance_UpdatesCorrectly()
    {
        var app1 = new ApplicationInfo("app1",
        [
            new InstanceInfoBuilder().WithAppName("app1").WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithAppName("app1").WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2",
        [
            new InstanceInfoBuilder().WithAppName("app2").WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build(),
            new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var delta = new ApplicationInfoCollection([
            new ApplicationInfo("app2",
            [
                new InstanceInfoBuilder().WithAppName("app2").WithId("id3").WithVipAddress("vapp2").WithSecureVipAddress("svapp2")
                    .WithActionType(ActionType.Added).Build()
            ])
        ]);

        apps.UpdateFromDelta(delta);

        ApplicationInfo? registered = apps.GetRegisteredApplication("app1");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app1");
        registered.Instances.Should().HaveCount(2);

        registered = apps.GetRegisteredApplication("app2");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app2");
        registered.Instances.Should().HaveCount(3);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp2");

        result.Should().HaveCount(3);
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().Contain(app2.GetInstance("id2")!);
        result.Should().Contain(app2.GetInstance("id3")!);

        result = apps.GetInstancesByVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromDelta_ExistingAppWithModifyInstance_UpdatesCorrectly()
    {
        var app1 = new ApplicationInfo("app1",
        [
            new InstanceInfoBuilder().WithAppName("app1").WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithAppName("app1").WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2",
        [
            new InstanceInfoBuilder().WithAppName("app2").WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").WithStatus(InstanceStatus.Up)
                .Build(),
            new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").WithStatus(InstanceStatus.Down)
                .Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var delta = new ApplicationInfoCollection([
            new ApplicationInfo("app2",
            [
                new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").WithStatus(InstanceStatus.Up)
                    .WithActionType(ActionType.Modified).Build()
            ])
        ]);

        apps.UpdateFromDelta(delta);

        ApplicationInfo? registered = apps.GetRegisteredApplication("app1");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app1");
        registered.Instances.Should().HaveCount(2);

        registered = apps.GetRegisteredApplication("app2");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app2");
        registered.Instances.Should().HaveCount(2);
        registered.Instances.Should().AllSatisfy(instance => instance.Status.Should().Be(InstanceStatus.Up));

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp2");

        result.Should().HaveCount(2);
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().Contain(app2.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void UpdateFromDelta_ExistingAppWithRemovedInstance_UpdatesCorrectly()
    {
        var app1 = new ApplicationInfo("app1",
        [
            new InstanceInfoBuilder().WithAppName("app1").WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build(),
            new InstanceInfoBuilder().WithAppName("app1").WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build()
        ]);

        var app2 = new ApplicationInfo("app2",
        [
            new InstanceInfoBuilder().WithAppName("app2").WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").WithStatus(InstanceStatus.Up)
                .Build(),
            new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").WithStatus(InstanceStatus.Down)
                .Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var delta = new ApplicationInfoCollection([
            new ApplicationInfo("app2",
            [
                new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2")
                    .WithActionType(ActionType.Deleted).Build()
            ])
        ]);

        apps.UpdateFromDelta(delta);

        ApplicationInfo? registered = apps.GetRegisteredApplication("app1");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app1");
        registered.Instances.Should().HaveCount(2);

        registered = apps.GetRegisteredApplication("app2");

        registered.Should().NotBeNull();
        registered.Name.Should().Be("app2");
        registered.Instances.Should().ContainSingle().Which.Status.Should().Be(InstanceStatus.Up);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        result.Should().HaveCount(2);
        result.Should().Contain(app1.GetInstance("id1")!);
        result.Should().Contain(app1.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("vapp2");

        result.Should().ContainSingle();
        result.Should().Contain(app2.GetInstance("id1")!);
        result.Should().NotContain(app2.GetInstance("id2")!);

        result = apps.GetInstancesByVipAddress("foobar");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ComputeHashCode_ReturnsExpected()
    {
        var app1 = new ApplicationInfo("app1",
        [
            new InstanceInfoBuilder().WithAppName("app1").WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").WithStatus(InstanceStatus.Down)
                .Build(),
            new InstanceInfoBuilder().WithAppName("app1").WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").WithStatus(InstanceStatus.Down)
                .Build()
        ]);

        var app2 = new ApplicationInfo("app2",
        [
            new InstanceInfoBuilder().WithAppName("app2").WithId("id1").WithVipAddress("vapp2").WithSecureVipAddress("svapp2").WithStatus(InstanceStatus.Up)
                .Build(),
            new InstanceInfoBuilder().WithAppName("app2").WithId("id2").WithVipAddress("vapp2").WithSecureVipAddress("svapp2")
                .WithStatus(InstanceStatus.OutOfService).Build()
        ]);

        var apps = new ApplicationInfoCollection([
            app1,
            app2
        ]);

        var delta = new ApplicationInfoCollection([
            new ApplicationInfo("app3",
            [
                new InstanceInfoBuilder().WithAppName("app3").WithId("id1").WithVipAddress("vapp3").WithSecureVipAddress("svapp3")
                    .WithActionType(ActionType.Added).WithStatus(InstanceStatus.Starting).Build()
            ])
        ]);

        apps.UpdateFromDelta(delta);

        string hashcode = apps.ComputeHashCode();
        hashcode.Should().Be("DOWN_2_OUT_OF_SERVICE_1_STARTING_1_UP_1_");
    }

    [Fact]
    public void FromJsonApplications_Correct()
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

        var applications = new JsonApplications
        {
            AppsHashCode = "AppsHashCode",
            VersionDelta = 1L,
            Applications = [application]
        };

        ApplicationInfoCollection apps = ApplicationInfoCollection.FromJson(applications, TimeProvider.System);

        apps.AppsHashCode.Should().Be("AppsHashCode");
        apps.Version.Should().Be(1);
        apps.ApplicationMap.Should().ContainSingle();

        ApplicationInfo? app = apps.GetRegisteredApplication("myApp");

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

    [Fact]
    public void FromJsonApplications_WithMissingInstanceId()
    {
        var instanceInfo = new JsonInstanceInfo
        {
            // InstanceId = "InstanceId",
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

        var applications = new JsonApplications
        {
            AppsHashCode = "AppsHashCode",
            VersionDelta = 1L,
            Applications = [application]
        };

        ApplicationInfoCollection apps = ApplicationInfoCollection.FromJson(applications, TimeProvider.System);

        apps.AppsHashCode.Should().Be("AppsHashCode");
        apps.Version.Should().Be(1);
        apps.ApplicationMap.Should().ContainSingle();

        ApplicationInfo? app = apps.GetRegisteredApplication("myApp");

        app.Should().NotBeNull();
        app.Name.Should().Be("myApp");
        app.Instances.Should().BeEmpty();
    }
}
