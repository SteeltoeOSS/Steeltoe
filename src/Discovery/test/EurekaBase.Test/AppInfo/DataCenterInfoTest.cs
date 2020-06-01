// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test
{
    public class DataCenterInfoTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_InitsName()
        {
            DataCenterInfo dinfo = new DataCenterInfo(DataCenterName.MyOwn);
            Assert.Equal(DataCenterName.MyOwn, dinfo.Name);
        }

        [Fact]
        public void ToJson_Correct()
        {
            DataCenterInfo dinfo = new DataCenterInfo(DataCenterName.MyOwn);
            var json = dinfo.ToJson();
            Assert.NotNull(json);
            Assert.Equal(DataCenterName.MyOwn.ToString(), json.Name);
            Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", json.ClassName);
        }

        [Fact]
        public void FromJson_Correct()
        {
            JsonInstanceInfo.JsonDataCenterInfo jinfo = new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", "MyOwn");
            var result = DataCenterInfo.FromJson(jinfo);
            Assert.Equal(DataCenterName.MyOwn, result.Name);
        }

        [Fact]
        public void FromJson_Throws_Invalid()
        {
            JsonInstanceInfo.JsonDataCenterInfo jinfo = new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", "FooBar");
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => DataCenterInfo.FromJson(jinfo));
            Assert.Contains("Datacenter", ex.Message);
        }
    }
}
