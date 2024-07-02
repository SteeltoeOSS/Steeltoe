// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka.Test.AppInfo;

public sealed class DataCenterInfoTest
{
    [Fact]
    public void Constructor_InitializesName()
    {
        var info = new DataCenterInfo
        {
            Name = DataCenterName.MyOwn
        };

        Assert.Equal(DataCenterName.MyOwn, info.Name);
    }

    [Fact]
    public void ToJson_Correct()
    {
        var info = new DataCenterInfo
        {
            Name = DataCenterName.MyOwn
        };

        JsonDataCenterInfo json = info.ToJson();
        Assert.NotNull(json);
        Assert.Equal(DataCenterName.MyOwn.ToString(), json.Name);
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", json.ClassName);
    }

    [Fact]
    public void FromJson_Correct()
    {
        var jsonInfo = new JsonDataCenterInfo
        {
            ClassName = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
            Name = "MyOwn"
        };

        DataCenterInfo? result = DataCenterInfo.FromJson(jsonInfo);
        Assert.NotNull(result);
        Assert.Equal(DataCenterName.MyOwn, result.Name);
    }

    [Fact]
    public void FromJson_Throws_Invalid()
    {
        var jsonInfo = new JsonDataCenterInfo
        {
            ClassName = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
            Name = "FooBar"
        };

        var ex = Assert.Throws<ArgumentException>(() => DataCenterInfo.FromJson(jsonInfo));
        Assert.Contains("Unsupported datacenter name", ex.Message, StringComparison.Ordinal);
    }
}
