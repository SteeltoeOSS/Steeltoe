// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test
{
    public class ApplicationTest : AbstractBaseTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            var app = new Application("foobar");
            Assert.Equal("foobar", app.Name);
            Assert.Equal(0, app.Count);
            Assert.NotNull(app.Instances);
            Assert.Equal(0, app.Instances.Count);
            Assert.Null(app.GetInstance("bar"));
        }

        [Fact]
        public void InstancesConstructor_InitializedCorrectly()
        {
            var infos = new List<InstanceInfo>()
            {
                new InstanceInfo() { InstanceId = "1" },
                new InstanceInfo() { InstanceId = "2" },
                new InstanceInfo() { InstanceId = "2" } // Note duplicate
            };

            var app = new Application("foobar", infos);

            Assert.Equal("foobar", app.Name);
            Assert.Equal(2, app.Count);
            Assert.NotNull(app.Instances);
            Assert.Equal(2, app.Instances.Count);
            Assert.Equal(2, app.Instances.Count);
            Assert.NotNull(app.GetInstance("1"));
            Assert.NotNull(app.GetInstance("2"));
        }

        [Fact]
        public void Add_Adds()
        {
            var app = new Application("foobar");
            var info = new InstanceInfo()
            {
                InstanceId = "1"
            };

            app.Add(info);

            Assert.NotNull(app.GetInstance("1"));
            Assert.True(app.GetInstance("1") == info);
            Assert.NotNull(app.Instances);
            Assert.Equal(1, app.Count);
            Assert.Equal(app.Count, app.Instances.Count);
        }

        [Fact]
        public void Add_Add_Updates()
        {
            var app = new Application("foobar");
            var info = new InstanceInfo()
            {
                InstanceId = "1",
                Status = InstanceStatus.DOWN
            };

            app.Add(info);

            Assert.NotNull(app.GetInstance("1"));
            Assert.Equal(InstanceStatus.DOWN, app.GetInstance("1").Status);

            var info2 = new InstanceInfo()
            {
                InstanceId = "1",
                Status = InstanceStatus.UP
            };

            app.Add(info2);
            Assert.Equal(1, app.Count);
            Assert.NotNull(app.GetInstance("1"));
            Assert.Equal(InstanceStatus.UP, app.GetInstance("1").Status);
        }
    }
}
