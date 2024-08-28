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

        Assert.NotNull(apps.ApplicationMap);
        Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
        Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
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

        Assert.NotNull(apps.ApplicationMap);
        Assert.Equal(2, apps.ApplicationMap.Count);
        Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
        Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
    }

    [Fact]
    public void Add_ExpandsTo_ApplicationMap()
    {
        var app1 = new ApplicationInfo("app1");
        app1.Add(new InstanceInfoBuilder().WithId("id1").WithVipAddress("vip1a,vip1b").Build());
        app1.Add(new InstanceInfoBuilder().WithId("id2").WithSecureVipAddress("svip2a,svip2b").Build());

        var apps = new ApplicationInfoCollection([app1]);

        Assert.Single(apps.GetInstancesByVipAddress("vip1a"));
        Assert.Single(apps.GetInstancesByVipAddress("vip1b"));
        Assert.Single(apps.GetInstancesBySecureVipAddress("svip2a"));
        Assert.Single(apps.GetInstancesBySecureVipAddress("svip2b"));
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

        Assert.NotNull(apps.ApplicationMap);
        Assert.Equal(2, apps.ApplicationMap.Count);
        Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
        Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
        ApplicationInfo app = apps.ApplicationMap["app1".ToUpperInvariant()];
        Assert.NotNull(app);
        IReadOnlyList<InstanceInfo> instances = app.Instances;
        Assert.NotNull(instances);

        foreach (InstanceInfo instance in instances)
        {
            Assert.True(instance.InstanceId is "id3" or "id4");
        }
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

        Assert.NotNull(apps.VipInstanceMap);
        Assert.Equal(2, apps.VipInstanceMap.Count);
        Assert.True(apps.VipInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
        Assert.True(apps.VipInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.VipInstanceMap["vapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.VipInstanceMap["vapp2".ToUpperInvariant()].Count);

        Assert.NotNull(apps.SecureVipInstanceMap);
        Assert.Equal(2, apps.SecureVipInstanceMap.Count);
        Assert.True(apps.SecureVipInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
        Assert.True(apps.SecureVipInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.SecureVipInstanceMap["svapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.SecureVipInstanceMap["svapp2".ToUpperInvariant()].Count);
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

        Assert.Equal(2, apps.Count);
        Assert.True(apps.ElementAt(0).Name is "app1" or "app2");
        Assert.True(apps.ElementAt(0).Name is "app1" or "app2");
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

        Assert.NotNull(apps.VipInstanceMap);
        Assert.Equal(2, apps.VipInstanceMap.Count);
        Assert.True(apps.VipInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
        Assert.True(apps.VipInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.VipInstanceMap["vapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.VipInstanceMap["vapp2".ToUpperInvariant()].Count);

        Assert.NotNull(apps.SecureVipInstanceMap);
        Assert.Equal(2, apps.SecureVipInstanceMap.Count);
        Assert.True(apps.SecureVipInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
        Assert.True(apps.SecureVipInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.SecureVipInstanceMap["svapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.SecureVipInstanceMap["svapp2".ToUpperInvariant()].Count);

        apps.RemoveInstanceFromVip(new InstanceInfoBuilder().WithId("id2").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build());
        apps.RemoveInstanceFromVip(new InstanceInfoBuilder().WithId("id1").WithVipAddress("vapp1").WithSecureVipAddress("svapp1").Build());

        Assert.NotNull(apps.VipInstanceMap);
        Assert.Single(apps.VipInstanceMap);
        Assert.False(apps.VipInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
        Assert.True(apps.VipInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
        Assert.False(apps.VipInstanceMap.TryGetValue("vapp1".ToUpperInvariant(), out _));
        Assert.Equal(2, apps.VipInstanceMap["vapp2".ToUpperInvariant()].Count);

        Assert.NotNull(apps.SecureVipInstanceMap);
        Assert.Single(apps.SecureVipInstanceMap);
        Assert.False(apps.SecureVipInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
        Assert.True(apps.SecureVipInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
        Assert.False(apps.SecureVipInstanceMap.TryGetValue("svapp1".ToUpperInvariant(), out _));
        Assert.Equal(2, apps.SecureVipInstanceMap["svapp2".ToUpperInvariant()].Count);
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
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);

        registered = apps.GetRegisteredApplication("foobar");
        Assert.Null(registered);
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

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesBySecureVipAddress("svapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.Contains(app2.GetInstance("id2"), result);

        result = apps.GetInstancesBySecureVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.Contains(app2.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.Contains(app2.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app3");
        Assert.NotNull(registered);
        Assert.Equal("app3", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Single(registered.Instances);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.Contains(app2.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp3");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(app3.GetInstance("id1"), result);

        result = apps.GetInstancesByVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(3, registered.Instances.Count);

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp2");
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.Contains(app2.GetInstance("id2"), result);
        Assert.Contains(app2.GetInstance("id3"), result);

        result = apps.GetInstancesByVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        foreach (InstanceInfo instance in registered.Instances)
        {
            Assert.Equal(InstanceStatus.Up, instance.Status);
        }

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.Contains(app2.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Single(registered.Instances);

        foreach (InstanceInfo instance in registered.Instances)
        {
            Assert.Equal(InstanceStatus.Up, instance.Status);
        }

        List<InstanceInfo> result = apps.GetInstancesByVipAddress("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(app1.GetInstance("id1"), result);
        Assert.Contains(app1.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("vapp2");
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(app2.GetInstance("id1"), result);
        Assert.DoesNotContain(app2.GetInstance("id2"), result);

        result = apps.GetInstancesByVipAddress("foobar");
        Assert.NotNull(result);
        Assert.Empty(result);
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
        Assert.Equal("DOWN_2_OUT_OF_SERVICE_1_STARTING_1_UP_1_", hashcode);
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

        var applications = new JsonApplications
        {
            AppsHashCode = "AppsHashCode",
            VersionDelta = 1L,
            Applications = [application]
        };

        ApplicationInfoCollection apps = ApplicationInfoCollection.FromJson(applications);

        Assert.Equal("AppsHashCode", apps.AppsHashCode);
        Assert.Equal(1, apps.Version);
        Assert.NotNull(apps.ApplicationMap);
        Assert.Single(apps.ApplicationMap);

        ApplicationInfo? app = apps.GetRegisteredApplication("myApp");

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

        var applications = new JsonApplications
        {
            AppsHashCode = "AppsHashCode",
            VersionDelta = 1L,
            Applications = [application]
        };

        ApplicationInfoCollection apps = ApplicationInfoCollection.FromJson(applications);

        Assert.Equal("AppsHashCode", apps.AppsHashCode);
        Assert.Equal(1, apps.Version);
        Assert.NotNull(apps.ApplicationMap);
        Assert.Single(apps.ApplicationMap);

        ApplicationInfo? app = apps.GetRegisteredApplication("myApp");

        Assert.NotNull(app);
        Assert.Equal("myApp", app.Name);
        Assert.NotNull(app.Instances);
        Assert.Empty(app.Instances);
    }
}
