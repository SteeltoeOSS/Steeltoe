// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
