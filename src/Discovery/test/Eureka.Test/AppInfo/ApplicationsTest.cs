// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test;

public class ApplicationsTest : AbstractBaseTest
{
    [Fact]
    public void ApplicationListConstructor__ThrowsIfListNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new Applications(null));
        Assert.Contains("apps", ex.Message);
    }

    [Fact]
    public void ApplicationListConstructor__AddsAppsFromList()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var apps = new Applications(new List<Application>
        {
            app1,
            app2
        });

        Assert.NotNull(apps.ApplicationMap);
        Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
        Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
    }

    [Fact]
    public void Add_ThrowsIfAppNull()
    {
        var apps = new Applications();
        var ex = Assert.Throws<ArgumentNullException>(() => apps.Add(null));
        Assert.Contains("app", ex.Message);
    }

    [Fact]
    public void Add__DoesNotAddAppWithNullInstanceId()
    {
        var app = new Application("app");

        app.Add(new InstanceInfo
        {
            InstanceId = null
        });

        Assert.Equal(0, app.Count);
    }

    [Fact]
    public void Add_AddsTo_ApplicationMap()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        Assert.NotNull(apps.ApplicationMap);
        Assert.Equal(2, apps.ApplicationMap.Count);
        Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
        Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
    }

    [Fact]
    public void Add_UpdatesExisting_ApplicationMap()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var apps = new Applications(new List<Application>
        {
            app1,
            app2
        });

        var app1Updated = new Application("app1");

        app1Updated.Add(new InstanceInfo
        {
            InstanceId = "id3"
        });

        app1Updated.Add(new InstanceInfo
        {
            InstanceId = "id4"
        });

        apps.Add(app1Updated);

        Assert.NotNull(apps.ApplicationMap);
        Assert.Equal(2, apps.ApplicationMap.Count);
        Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
        Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
        Application app = apps.ApplicationMap["app1".ToUpperInvariant()];
        Assert.NotNull(app);
        IList<InstanceInfo> instances = app.Instances;
        Assert.NotNull(instances);

        foreach (InstanceInfo instance in instances)
        {
            Assert.True(instance.InstanceId.Equals("id3") || instance.InstanceId.Equals("id4"));
        }
    }

    [Fact]
    public void Add_AddsTo_VirtualHostInstanceMaps()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        Assert.NotNull(apps.VirtualHostInstanceMap);
        Assert.Equal(2, apps.VirtualHostInstanceMap.Count);
        Assert.True(apps.VirtualHostInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
        Assert.True(apps.VirtualHostInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.VirtualHostInstanceMap["vapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.VirtualHostInstanceMap["vapp2".ToUpperInvariant()].Count);

        Assert.NotNull(apps.SecureVirtualHostInstanceMap);
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap.Count);
        Assert.True(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
        Assert.True(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap["svapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap["svapp2".ToUpperInvariant()].Count);
    }

    [Fact]
    public void GetRegisteredApplications_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var apps = new Applications(new List<Application>
        {
            app1,
            app2
        });

        IList<Application> registered = apps.GetRegisteredApplications();
        Assert.NotNull(registered);
        Assert.Equal(2, registered.Count);
        Assert.True(registered[0].Name.Equals("app1") || registered[0].Name.Equals("app2"));
        Assert.True(registered[1].Name.Equals("app1") || registered[1].Name.Equals("app2"));
    }

    [Fact]
    public void RemoveInstanceFromVip_UpdatesApp_RemovesFromVirtualHostInstanceMaps()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        Assert.NotNull(apps.VirtualHostInstanceMap);
        Assert.Equal(2, apps.VirtualHostInstanceMap.Count);
        Assert.True(apps.VirtualHostInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
        Assert.True(apps.VirtualHostInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.VirtualHostInstanceMap["vapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.VirtualHostInstanceMap["vapp2".ToUpperInvariant()].Count);

        Assert.NotNull(apps.SecureVirtualHostInstanceMap);
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap.Count);
        Assert.True(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
        Assert.True(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap["svapp1".ToUpperInvariant()].Count);
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap["svapp2".ToUpperInvariant()].Count);

        apps.RemoveInstanceFromVip(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        apps.RemoveInstanceFromVip(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        Assert.NotNull(apps.VirtualHostInstanceMap);
        Assert.Single(apps.VirtualHostInstanceMap);
        Assert.False(apps.VirtualHostInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
        Assert.True(apps.VirtualHostInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
        Assert.False(apps.VirtualHostInstanceMap.TryGetValue("vapp1".ToUpperInvariant(), out _));
        Assert.Equal(2, apps.VirtualHostInstanceMap["vapp2".ToUpperInvariant()].Count);

        Assert.NotNull(apps.SecureVirtualHostInstanceMap);
        Assert.Single(apps.SecureVirtualHostInstanceMap);
        Assert.False(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
        Assert.True(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
        Assert.False(apps.SecureVirtualHostInstanceMap.TryGetValue("svapp1".ToUpperInvariant(), out _));
        Assert.Equal(2, apps.SecureVirtualHostInstanceMap["svapp2".ToUpperInvariant()].Count);
    }

    [Fact]
    public void GetRegisteredApplication_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2"
        });

        var apps = new Applications(new List<Application>
        {
            app1,
            app2
        });

        Application registered = apps.GetRegisteredApplication("app1");
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);

        registered = apps.GetRegisteredApplication("foobar");
        Assert.Null(registered);
    }

    [Fact]
    public void GetRegisteredApplication_ThrowsIfAppNull()
    {
        var apps = new Applications();
        var ex = Assert.Throws<ArgumentNullException>(() => apps.GetRegisteredApplication(null));
        Assert.Contains("appName", ex.Message);
    }

    [Fact]
    public void GetInstancesBySecureVirtualHostName_ThrowsIfAddressNull()
    {
        var apps = new Applications();
        var ex = Assert.Throws<ArgumentNullException>(() => apps.GetInstancesBySecureVirtualHostName(null));
        Assert.Contains("secureVirtualHostName", ex.Message);
    }

    [Fact]
    public void GetInstancesByVirtualHostName_ThrowsIfAddressNull()
    {
        var apps = new Applications();
        var ex = Assert.Throws<ArgumentNullException>(() => apps.GetInstancesByVirtualHostName(null));
        Assert.Contains("virtualHostName", ex.Message);
    }

    [Fact]
    public void GetInstancesBySecureVirtualHostName_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        IList<InstanceInfo> result = apps.GetInstancesBySecureVirtualHostName("svapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesBySecureVirtualHostName("svapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.True(result.Contains(app2.GetInstance("id2")));

        result = apps.GetInstancesBySecureVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void GetInstancesByVirtualHostName_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        IList<InstanceInfo> result = apps.GetInstancesByVirtualHostName("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.True(result.Contains(app2.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void UpdateFromDelta_EmptyDelta_NoChange()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var delta = new Applications();
        apps.UpdateFromDelta(delta);

        Application registered = apps.GetRegisteredApplication("app1");
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        IList<InstanceInfo> result = apps.GetInstancesByVirtualHostName("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.True(result.Contains(app2.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void UpdateFromDelta_AddNewAppNewInstance_UpdatesCorrectly()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var delta = new Applications();
        var app3 = new Application("app3");

        app3.Add(new InstanceInfo
        {
            AppName = "app3",
            InstanceId = "id1",
            VipAddress = "vapp3",
            SecureVipAddress = "svapp3",
            ActionType = ActionType.Added
        });

        delta.Add(app3);
        apps.UpdateFromDelta(delta);

        Application registered = apps.GetRegisteredApplication("app1");
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
        Assert.Equal(1, registered.Instances.Count);

        IList<InstanceInfo> result = apps.GetInstancesByVirtualHostName("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.True(result.Contains(app2.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp3");
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.True(result.Contains(app3.GetInstance("id1")));

        result = apps.GetInstancesByVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void UpdateFromDelta_ExistingAppWithAddNewInstance_UpdatesCorrectly()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2"
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var delta = new Applications();
        var deltaApp3 = new Application("app2");

        deltaApp3.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id3",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            ActionType = ActionType.Added
        });

        delta.Add(deltaApp3);
        apps.UpdateFromDelta(delta);

        Application registered = apps.GetRegisteredApplication("app1");
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(3, registered.Instances.Count);

        IList<InstanceInfo> result = apps.GetInstancesByVirtualHostName("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp2");
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.True(result.Contains(app2.GetInstance("id2")));
        Assert.True(result.Contains(app2.GetInstance("id3")));

        result = apps.GetInstancesByVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void UpdateFromDelta_ExistingAppWithModifyInstance_UpdatesCorrectly()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Down
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var delta = new Applications();
        var deltaApp3 = new Application("app2");

        deltaApp3.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up,
            ActionType = ActionType.Modified
        });

        delta.Add(deltaApp3);
        apps.UpdateFromDelta(delta);

        Application registered = apps.GetRegisteredApplication("app1");
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        foreach (InstanceInfo inst in registered.Instances)
        {
            Assert.Equal(InstanceStatus.Up, inst.Status);
        }

        IList<InstanceInfo> result = apps.GetInstancesByVirtualHostName("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.True(result.Contains(app2.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void UpdateFromDelta_ExistingAppWithRemovedInstance_UpdatesCorrectly()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1"
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Down
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var delta = new Applications();
        var deltaApp3 = new Application("app2");

        deltaApp3.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            ActionType = ActionType.Deleted
        });

        delta.Add(deltaApp3);
        apps.UpdateFromDelta(delta);

        Application registered = apps.GetRegisteredApplication("app1");
        Assert.NotNull(registered);
        Assert.Equal("app1", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(2, registered.Instances.Count);

        registered = apps.GetRegisteredApplication("app2");
        Assert.NotNull(registered);
        Assert.Equal("app2", registered.Name);
        Assert.NotNull(registered.Instances);
        Assert.Equal(1, registered.Instances.Count);

        foreach (InstanceInfo inst in registered.Instances)
        {
            Assert.Equal(InstanceStatus.Up, inst.Status);
        }

        IList<InstanceInfo> result = apps.GetInstancesByVirtualHostName("vapp1");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.Contains(app1.GetInstance("id1")));
        Assert.True(result.Contains(app1.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("vapp2");
        Assert.NotNull(result);
        Assert.Equal(1, result.Count);
        Assert.True(result.Contains(app2.GetInstance("id1")));
        Assert.False(result.Contains(app2.GetInstance("id2")));

        result = apps.GetInstancesByVirtualHostName("foobar");
        Assert.NotNull(result);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void ComputeHashCode_ReturnsExpected()
    {
        var app1 = new Application("app1");

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id1",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        app1.Add(new InstanceInfo
        {
            AppName = "app1",
            InstanceId = "id2",
            VipAddress = "vapp1",
            SecureVipAddress = "svapp1",
            Status = InstanceStatus.Down
        });

        var app2 = new Application("app2");

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id1",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.Up
        });

        app2.Add(new InstanceInfo
        {
            AppName = "app2",
            InstanceId = "id2",
            VipAddress = "vapp2",
            SecureVipAddress = "svapp2",
            Status = InstanceStatus.OutOfService
        });

        var apps = new Applications();
        apps.Add(app1);
        apps.Add(app2);

        var delta = new Applications();
        var app3 = new Application("app3");

        app3.Add(new InstanceInfo
        {
            AppName = "app3",
            InstanceId = "id1",
            VipAddress = "vapp3",
            SecureVipAddress = "svapp3",
            ActionType = ActionType.Added,
            Status = InstanceStatus.Starting
        });

        delta.Add(app3);
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
            IpAddress = "IpAddr",
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

        var application = new JsonApplication
        {
            Name = "myApp",
            Instances = new List<JsonInstanceInfo>
            {
                instanceInfo
            }
        };

        var applications = new JsonApplications
        {
            AppsHashCode = "AppsHashCode",
            VersionDelta = 1L,
            Applications = new List<JsonApplication>
            {
                application
            }
        };

        var apps = Applications.FromJsonApplications(applications);

        Assert.Equal("AppsHashCode", apps.AppsHashCode);
        Assert.Equal(1, apps.Version);
        Assert.NotNull(apps.ApplicationMap);
        Assert.Single(apps.ApplicationMap);

        Application app = apps.GetRegisteredApplication("myApp");

        // Verify
        Assert.NotNull(app);
        Assert.Equal("myApp", app.Name);
        Assert.NotNull(app.Instances);
        Assert.Equal(1, app.Count);
        Assert.Equal(1, app.Instances.Count);
        Assert.NotNull(app.GetInstance("InstanceId"));
        InstanceInfo info = app.GetInstance("InstanceId");

        Assert.Equal("InstanceId", info.InstanceId);
        Assert.Equal("myApp", info.AppName);
        Assert.Equal("AppGroupName", info.AppGroupName);
        Assert.Equal("IpAddr", info.IpAddress);
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
    public void FromJsonApplications_WithMissingInstanceId()
    {
        var instanceInfo = new JsonInstanceInfo
        {
            // InstanceId = "InstanceId",
            AppName = "myApp",
            AppGroupName = "AppGroupName",
            IpAddress = "IpAddr",
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

        var application = new JsonApplication
        {
            Name = "myApp",
            Instances = new List<JsonInstanceInfo>
            {
                instanceInfo
            }
        };

        var applications = new JsonApplications
        {
            AppsHashCode = "AppsHashCode",
            VersionDelta = 1L,
            Applications = new List<JsonApplication>
            {
                application
            }
        };

        var apps = Applications.FromJsonApplications(applications);

        Assert.Equal("AppsHashCode", apps.AppsHashCode);
        Assert.Equal(1, apps.Version);
        Assert.NotNull(apps.ApplicationMap);
        Assert.Single(apps.ApplicationMap);

        Application app = apps.GetRegisteredApplication("myApp");

        // Verify
        Assert.NotNull(app);
        Assert.Equal("myApp", app.Name);
        Assert.NotNull(app.Instances);
        Assert.Equal(1, app.Count);
        Assert.Equal(1, app.Instances.Count);
        Assert.Null(app.GetInstance("InstanceId"));
    }
}
