//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using SteelToe.Discovery.Eureka.Client.Test;
using SteelToe.Discovery.Eureka.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;

namespace SteelToe.Discovery.Eureka.AppInfo.Test
{
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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1" });
            app2.Add(new InstanceInfo() { InstanceId = "id2" });

            var apps = new Applications(new List<Application>() { app1, app2 });

            Assert.NotNull(apps.ApplicationMap);
            Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
            Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
        }

        [Fact]
        public void Add_ThrowsIfAppNull()
        {
            Applications apps = new Applications();
            var ex = Assert.Throws<ArgumentNullException>(() => apps.Add(null));
            Assert.Contains("app", ex.Message);
        }

        [Fact]
        public void Add_AddsTo_ApplicationMap()
        {
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1" });
            app2.Add(new InstanceInfo() { InstanceId = "id2" });

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1" });
            app2.Add(new InstanceInfo() { InstanceId = "id2" });

            var apps = new Applications(new List<Application>() { app1, app2 });

            Application app1updated = new Application("app1");
            app1updated.Add(new InstanceInfo() { InstanceId = "id3" });
            app1updated.Add(new InstanceInfo() { InstanceId = "id4" });

            apps.Add(app1updated);

            Assert.NotNull(apps.ApplicationMap);
            Assert.Equal(2, apps.ApplicationMap.Count);
            Assert.True(apps.ApplicationMap.ContainsKey("app1".ToUpperInvariant()));
            Assert.True(apps.ApplicationMap.ContainsKey("app2".ToUpperInvariant()));
            var app = apps.ApplicationMap["app1".ToUpperInvariant()];
            Assert.NotNull(app);
            var instances = app.Instances;
            Assert.NotNull(instances);
            foreach (var instance in instances)
            {
                Assert.True(instance.InstanceId.Equals("id3") || instance.InstanceId.Equals("id4"));
            }
        }

        [Fact]
        public void Add_AddsTo_VirtualHostInstanceMaps()
        {
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1" });
            app2.Add(new InstanceInfo() { InstanceId = "id2" });

            var apps = new Applications(new List<Application>() { app1, app2 });

            var registered = apps.GetRegisteredApplications();
            Assert.NotNull(registered);
            Assert.Equal(2, registered.Count);
            Assert.True(registered[0].Name.Equals("app1") || registered[0].Name.Equals("app2"));
            Assert.True(registered[1].Name.Equals("app1") || registered[1].Name.Equals("app2"));
        }


        [Fact]
        public void RemoveInstanceFromVip_UpdatesApp_RemovesFromVirtualHostInstanceMaps()
        {
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

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

            apps.RemoveInstanceFromVip(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            apps.RemoveInstanceFromVip(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });


            Assert.NotNull(apps.VirtualHostInstanceMap);
            Assert.Equal(1, apps.VirtualHostInstanceMap.Count);
            Assert.False(apps.VirtualHostInstanceMap.ContainsKey("vapp1".ToUpperInvariant()));
            Assert.True(apps.VirtualHostInstanceMap.ContainsKey("vapp2".ToUpperInvariant()));
            ConcurrentDictionary<string, InstanceInfo> tryValue = null;
            Assert.False(apps.VirtualHostInstanceMap.TryGetValue("vapp1".ToUpperInvariant(), out tryValue));
            Assert.Equal(2, apps.VirtualHostInstanceMap["vapp2".ToUpperInvariant()].Count);

            Assert.NotNull(apps.SecureVirtualHostInstanceMap);
            Assert.Equal(1, apps.SecureVirtualHostInstanceMap.Count);
            Assert.False(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp1".ToUpperInvariant()));
            Assert.True(apps.SecureVirtualHostInstanceMap.ContainsKey("svapp2".ToUpperInvariant()));
            tryValue = null;
            Assert.False(apps.SecureVirtualHostInstanceMap.TryGetValue("svapp1".ToUpperInvariant(), out tryValue));
            Assert.Equal(2, apps.SecureVirtualHostInstanceMap["svapp2".ToUpperInvariant()].Count);

        }

        [Fact]
        public void GetRegisteredApplication_ReturnsExpected()
        {
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1" });
            app2.Add(new InstanceInfo() { InstanceId = "id2" });

            var apps = new Applications(new List<Application>() { app1, app2 });

            var registered = apps.GetRegisteredApplication("app1");
            Assert.NotNull(registered);
            Assert.Equal("app1", registered.Name);

            registered = apps.GetRegisteredApplication("foobar");
            Assert.Null(registered);

        }
        [Fact]
        public void GetRegisteredApplication_ThrowsIfAppNull()
        {
            Applications apps = new Applications();
            var ex = Assert.Throws<ArgumentException>(() => apps.GetRegisteredApplication(null));
            Assert.Contains("appName", ex.Message);
        }

        [Fact]
        public void GetInstancesBySecureVirtualHostName_ThrowsIfAddressNull()
        {
            Applications apps = new Applications();
            var ex = Assert.Throws<ArgumentException>(() => apps.GetInstancesBySecureVirtualHostName(null));
            Assert.Contains("secureVirtualHostName", ex.Message);
        }

        [Fact]
        public void GetInstancesByVirtualHostName_ThrowsIfAddressNull()
        {
            Applications apps = new Applications();
            var ex = Assert.Throws<ArgumentException>(() => apps.GetInstancesByVirtualHostName(null));
            Assert.Contains("virtualHostName", ex.Message);
        }
        [Fact]
        public void GetInstancesBySecureVirtualHostName_ReturnsExpected()
        {
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var result = apps.GetInstancesBySecureVirtualHostName("svapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var result = apps.GetInstancesByVirtualHostName("vapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var delta = new Applications();
            apps.UpdateFromDelta(delta);

            var registered = apps.GetRegisteredApplication("app1");
            Assert.NotNull(registered);
            Assert.Equal("app1", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(2, registered.Instances.Count);

            registered = apps.GetRegisteredApplication("app2");
            Assert.NotNull(registered);
            Assert.Equal("app2", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(2, registered.Instances.Count);

            var result = apps.GetInstancesByVirtualHostName("vapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var delta = new Applications();
            Application app3 = new Application("app3");
            app3.Add(new InstanceInfo() { AppName = "app3", InstanceId = "id1", VipAddress = "vapp3", SecureVipAddress = "svapp3", Actiontype = ActionType.ADDED });
            delta.Add(app3);
            apps.UpdateFromDelta(delta);

            var registered = apps.GetRegisteredApplication("app1");
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

            var result = apps.GetInstancesByVirtualHostName("vapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2" });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2" });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var delta = new Applications();
            Application deltaApp3 = new Application("app2");
            deltaApp3.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id3", VipAddress = "vapp2", SecureVipAddress = "svapp2", Actiontype = ActionType.ADDED });
            delta.Add(deltaApp3);
            apps.UpdateFromDelta(delta);

            var registered = apps.GetRegisteredApplication("app1");
            Assert.NotNull(registered);
            Assert.Equal("app1", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(2, registered.Instances.Count);

            registered = apps.GetRegisteredApplication("app2");
            Assert.NotNull(registered);
            Assert.Equal("app2", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(3, registered.Instances.Count);

            var result = apps.GetInstancesByVirtualHostName("vapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.DOWN });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var delta = new Applications();
            Application deltaApp3 = new Application("app2");
            deltaApp3.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP, Actiontype = ActionType.MODIFIED });
            delta.Add(deltaApp3);
            apps.UpdateFromDelta(delta);

            var registered = apps.GetRegisteredApplication("app1");
            Assert.NotNull(registered);
            Assert.Equal("app1", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(2, registered.Instances.Count);

            registered = apps.GetRegisteredApplication("app2");
            Assert.NotNull(registered);
            Assert.Equal("app2", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(2, registered.Instances.Count);
            foreach (var inst in registered.Instances)
            {
                Assert.Equal(InstanceStatus.UP, inst.Status);
            }

            var result = apps.GetInstancesByVirtualHostName("vapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1" });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1" });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.DOWN });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var delta = new Applications();
            Application deltaApp3 = new Application("app2");
            deltaApp3.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Actiontype = ActionType.DELETED });
            delta.Add(deltaApp3);
            apps.UpdateFromDelta(delta);

            var registered = apps.GetRegisteredApplication("app1");
            Assert.NotNull(registered);
            Assert.Equal("app1", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(2, registered.Instances.Count);

            registered = apps.GetRegisteredApplication("app2");
            Assert.NotNull(registered);
            Assert.Equal("app2", registered.Name);
            Assert.NotNull(registered.Instances);
            Assert.Equal(1, registered.Instances.Count);
            foreach (var inst in registered.Instances)
            {
                Assert.Equal(InstanceStatus.UP, inst.Status);
            }

            var result = apps.GetInstancesByVirtualHostName("vapp1");

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
            Application app1 = new Application("app1");
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id1", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });
            app1.Add(new InstanceInfo() { AppName = "app1", InstanceId = "id2", VipAddress = "vapp1", SecureVipAddress = "svapp1", Status = InstanceStatus.DOWN });

            Application app2 = new Application("app2");
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id1", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.UP });
            app2.Add(new InstanceInfo() { AppName = "app2", InstanceId = "id2", VipAddress = "vapp2", SecureVipAddress = "svapp2", Status = InstanceStatus.OUT_OF_SERVICE });

            var apps = new Applications();
            apps.Add(app1);
            apps.Add(app2);

            var delta = new Applications();
            Application app3 = new Application("app3");
            app3.Add(new InstanceInfo() { AppName = "app3", InstanceId = "id1", VipAddress = "vapp3", SecureVipAddress = "svapp3", Actiontype = ActionType.ADDED, Status = InstanceStatus.STARTING });
            delta.Add(app3);
            apps.UpdateFromDelta(delta);


            string hashcode = apps.ComputeHashCode();
            Assert.Equal("DOWN_2_OUT_OF_SERVICE_1_STARTING_1_UP_1_", hashcode);
        }

        [Fact]
        public void FromJsonApplications_Correct()
        {
            JsonInstanceInfo jinfo = new JsonInstanceInfo()
            {
                InstanceId = "InstanceId",
                AppName = "myApp",
                AppGroupName = "AppGroupName",
                IpAddr = "IpAddr",
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
                DataCenterInfo = new JsonInstanceInfo.JsonDataCenterInfo("", "MyOwn"),
                HostName = "HostName",
                Status = InstanceStatus.DOWN,
                OverriddenStatus = InstanceStatus.OUT_OF_SERVICE,
                LeaseInfo = new JsonLeaseInfo()
                {
                    RenewalIntervalInSecs = 1,
                    DurationInSecs = 2,
                    RegistrationTimestamp = 1457973741708,
                    LastRenewalTimestamp = 1457973741708,
                    LastRenewalTimestampLegacy = 1457973741708,
                    EvictionTimestamp = 1457973741708,
                    ServiceUpTimestamp = 1457973741708
                },
                IsCoordinatingDiscoveryServer = false,
                Metadata = new Dictionary<string, string>() { { "@class", "java.util.Collections$EmptyMap" } },
                LastUpdatedTimestamp = 1457973741708,
                LastDirtyTimestamp = 1457973741708,
                Actiontype = ActionType.ADDED,
                AsgName = "AsgName"
            };
            JsonApplication japp = new JsonApplication("myApp", new List<JsonInstanceInfo> { jinfo });
            JsonApplications japps = new JsonApplications("AppsHashCode", 1L, new List<JsonApplication>() { japp });

            Applications apps = Applications.FromJsonApplications(japps);

            Assert.Equal("AppsHashCode", apps.AppsHashCode);
            Assert.Equal(1, apps.Version);
            Assert.NotNull(apps.ApplicationMap);
            Assert.Equal(1, apps.ApplicationMap.Count);

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
            Assert.Equal("IpAddr", info.IpAddr);
            Assert.Equal("Sid", info.Sid);
            Assert.Equal(100, info.Port);
            Assert.True(info.IsUnsecurePortEnabled);
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
            Assert.Equal(InstanceStatus.DOWN, info.Status);
            Assert.Equal(InstanceStatus.OUT_OF_SERVICE, info.OverriddenStatus);
            Assert.NotNull(info.LeaseInfo);
            Assert.Equal(1, info.LeaseInfo.RenewalIntervalInSecs);
            Assert.Equal(2, info.LeaseInfo.DurationInSecs);
            Assert.Equal(635935705417080000L, info.LeaseInfo.RegistrationTimestamp);
            Assert.Equal(635935705417080000L, info.LeaseInfo.LastRenewalTimestamp);
            Assert.Equal(635935705417080000L, info.LeaseInfo.LastRenewalTimestampLegacy);
            Assert.Equal(635935705417080000L, info.LeaseInfo.EvictionTimestamp);
            Assert.Equal(635935705417080000L, info.LeaseInfo.ServiceUpTimestamp);
            Assert.False(info.IsCoordinatingDiscoveryServer);
            Assert.NotNull(info.Metadata);
            Assert.Equal(0, info.Metadata.Count);
            Assert.Equal(635935705417080000L, info.LastUpdatedTimestamp);
            Assert.Equal(635935705417080000L, info.LastDirtyTimestamp);
            Assert.Equal(ActionType.ADDED, info.Actiontype);
            Assert.Equal("AsgName", info.AsgName);
        }
    }
}
