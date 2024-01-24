// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test.Transport;

public sealed class JsonInstanceInfoTest : AbstractBaseTest
{
    [Fact]
    public void Deserialize_GoodJson()
    {
        const string json = @"
{ 
    ""instanceId"":""localhost:foo"",
    ""hostName"":""localhost"",
    ""app"":""FOO"",
    ""ipAddr"":""192.168.56.1"",
    ""status"":""UP"",
    ""overriddenstatus"":""UNKNOWN"",
    ""port"":{""$"":8080,""@enabled"":""true""},
    ""securePort"":{""$"":443,""@enabled"":""false""},
    ""countryId"":1,
    ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
    ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1457714988223,""lastRenewalTimestamp"":1457716158319,""evictionTimestamp"":0,""serviceUpTimestamp"":1457714988223},
    ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
    ""homePageUrl"":""http://localhost:8080/"",
    ""statusPageUrl"":""http://localhost:8080/info"",
    ""healthCheckUrl"":""http://localhost:8080/health"",
    ""vipAddress"":""foo"",
    ""isCoordinatingDiscoveryServer"":""false"",
    ""lastUpdatedTimestamp"":""1457714988223"",
    ""lastDirtyTimestamp"":""1457714988172"",
    ""actionType"":""ADDED""
}";

        var result = JsonSerializer.Deserialize<JsonInstanceInfo>(json);

        Assert.NotNull(result);
        Assert.Equal("localhost:foo", result.InstanceId);
        Assert.Equal("localhost", result.HostName);
        Assert.Equal("FOO", result.AppName);
        Assert.Equal("192.168.56.1", result.IPAddress);
        Assert.Equal(InstanceStatus.Up, result.Status);
        Assert.Equal(InstanceStatus.Unknown, result.OverriddenStatus);
        Assert.NotNull(result.Port);
        Assert.True(result.Port.Enabled);
        Assert.Equal(8080, result.Port.Port);
        Assert.NotNull(result.SecurePort);
        Assert.False(result.SecurePort.Enabled);
        Assert.Equal(443, result.SecurePort.Port);
        Assert.Equal(1, result.CountryId);
        Assert.NotNull(result.DataCenterInfo);
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", result.DataCenterInfo.ClassName);
        Assert.Equal("MyOwn", result.DataCenterInfo.Name);
        Assert.NotNull(result.LeaseInfo);
        Assert.Equal(30, result.LeaseInfo.RenewalIntervalInSecs);
        Assert.Equal(90, result.LeaseInfo.DurationInSecs);
        Assert.Equal(1_457_714_988_223, result.LeaseInfo.RegistrationTimestamp);
        Assert.Equal(1_457_716_158_319, result.LeaseInfo.LastRenewalTimestamp);
        Assert.Equal(0, result.LeaseInfo.EvictionTimestamp);
        Assert.Equal(1_457_714_988_223, result.LeaseInfo.ServiceUpTimestamp);
        Assert.NotNull(result.Metadata);
        Assert.True(result.Metadata.Count == 1);
        Assert.True(result.Metadata.ContainsKey("@class"));
        Assert.Equal("java.util.Collections$EmptyMap", result.Metadata["@class"]);
        Assert.Equal("http://localhost:8080/", result.HomePageUrl);
        Assert.Equal("http://localhost:8080/info", result.StatusPageUrl);
        Assert.Equal("http://localhost:8080/health", result.HealthCheckUrl);
        Assert.Equal("foo", result.VipAddress);
        Assert.False(result.IsCoordinatingDiscoveryServer);
        Assert.Equal(1_457_714_988_223, result.LastUpdatedTimestamp);
        Assert.Equal(1_457_714_988_172, result.LastDirtyTimestamp);
        Assert.Equal(ActionType.Added, result.ActionType);
    }
}
