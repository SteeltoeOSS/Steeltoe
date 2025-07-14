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

        info.Name.Should().Be(DataCenterName.MyOwn);
    }

    [Fact]
    public void ToJson_Correct()
    {
        var info = new DataCenterInfo
        {
            Name = DataCenterName.MyOwn
        };

        JsonDataCenterInfo json = info.ToJson();

        json.Should().NotBeNull();
        json.Name.Should().Be(nameof(DataCenterName.MyOwn));
        json.ClassName.Should().Be("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo");
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

        result.Should().NotBeNull();
        result.Name.Should().Be(DataCenterName.MyOwn);
    }

    [Fact]
    public void FromJson_Throws_Invalid()
    {
        var jsonInfo = new JsonDataCenterInfo
        {
            ClassName = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo",
            Name = "FooBar"
        };

        Action action = () => DataCenterInfo.FromJson(jsonInfo);

        action.Should().ThrowExactly<ArgumentException>().WithMessage("Unsupported datacenter name*");
    }
}
