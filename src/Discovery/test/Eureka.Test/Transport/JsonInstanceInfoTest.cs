// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Test;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Transport.Test;

public class JsonInstanceInfoTest : AbstractBaseTest
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
        Assert.Equal("192.168.56.1", result.IpAddress);
        Assert.Equal(InstanceStatus.Up, result.Status);
        Assert.Equal(InstanceStatus.Unknown, result.OverriddenStatus);
        JsonInstanceInfo.JsonPortWrapper port = result.Port;
        Assert.True(port.Enabled);
        Assert.Equal(8080, port.Port);
        JsonInstanceInfo.JsonPortWrapper securePort = result.SecurePort;
        Assert.False(securePort.Enabled);
        Assert.Equal(443, securePort.Port);
        Assert.Equal(1, result.CountryId);
        JsonInstanceInfo.JsonDataCenterInfo dataCenterInfo = result.DataCenterInfo;
        Assert.Equal("com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", dataCenterInfo.ClassName);
        Assert.Equal("MyOwn", dataCenterInfo.Name);
        JsonLeaseInfo leaseInfo = result.LeaseInfo;
        Assert.Equal(30, leaseInfo.RenewalIntervalInSecs);
        Assert.Equal(90, leaseInfo.DurationInSecs);
        Assert.Equal(1_457_714_988_223, leaseInfo.RegistrationTimestamp);
        Assert.Equal(1_457_716_158_319, leaseInfo.LastRenewalTimestamp);
        Assert.Equal(0, leaseInfo.EvictionTimestamp);
        Assert.Equal(1_457_714_988_223, leaseInfo.ServiceUpTimestamp);
        Dictionary<string, string> metadata = result.Metadata;
        Assert.NotNull(metadata);
        Assert.True(metadata.Count == 1);
        Assert.True(metadata.ContainsKey("@class"));
        Assert.True(metadata.ContainsValue("java.util.Collections$EmptyMap"));
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
