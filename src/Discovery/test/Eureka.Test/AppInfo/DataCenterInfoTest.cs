// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test;

public class DataCenterInfoTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_InitsName()
    {
        var dinfo = new DataCenterInfo(DataCenterName.MyOwn);
        Assert.Equal(DataCenterName.MyOwn, dinfo.Name);
    }

    [Fact]
    public void ToJson_Correct()
    {
        var dinfo = new DataCenterInfo(DataCenterName.MyOwn);
        var json = dinfo.ToJson();
        Assert.NotNull(json);
        Assert.Equal(DataCenterName.MyOwn.ToString(), json.Name);
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", json.ClassName);
    }

    [Theory]
    [InlineData("Netflix")]
    [InlineData("Amazon")]
    [InlineData("MyOwn")]
    public void FromJson_Correct(string name)
    {
        var jinfo = new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", name);
        var expected = Enum.Parse<DataCenterName>(name);
        var result = DataCenterInfo.FromJson(jinfo);
        Assert.Equal(expected, result.Name);
    }

    [Fact]
    public void FromJson_ReturnsNull_WhenInvalid()
    {
        var jinfo = new JsonInstanceInfo.JsonDataCenterInfo("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", "FooBar");
        var result = DataCenterInfo.FromJson(jinfo);
        Assert.Null(result);
    }
}