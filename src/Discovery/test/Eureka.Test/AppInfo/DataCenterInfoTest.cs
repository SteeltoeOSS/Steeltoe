// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public class DataCenterInfoTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_InitializesName()
    {
        var info = new DataCenterInfo(DataCenterName.MyOwn);
        Assert.Equal(DataCenterName.MyOwn, info.Name);
    }

    [Fact]
    public void ToJson_Correct()
    {
        var info = new DataCenterInfo(DataCenterName.MyOwn);
        JsonInstanceInfo.JsonDataCenterInfo json = info.ToJson();
        Assert.NotNull(json);
        Assert.Equal(DataCenterName.MyOwn.ToString(), json.Name);
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", json.ClassName);
    }

    [Fact]
    public void FromJson_Correct()
    {
        var info = new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", "MyOwn");
        IDataCenterInfo result = DataCenterInfo.FromJson(info);
        Assert.Equal(DataCenterName.MyOwn, result.Name);
    }

    [Fact]
    public void FromJson_Throws_Invalid()
    {
        var info = new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", "FooBar");
        var ex = Assert.Throws<ArgumentException>(() => DataCenterInfo.FromJson(info));
        Assert.Contains("Unsupported datacenter name", ex.Message);
    }
}
