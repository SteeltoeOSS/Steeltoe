// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System.Text.Json;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Client.Test.Transport;

public class JsonApplicationRootTest : AbstractBaseTest
{
    [Fact]
    public void Deserialize_GoodJson()
    {
        var json = @"
                {
                    ""application"":
                    {
                        ""name"":""FOO"",
                        ""instance"":[
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
                            ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":1458152330783,""lastRenewalTimestamp"":1458243422342,""evictionTimestamp"":0,""serviceUpTimestamp"":1458152330783},
                            ""metadata"":{""@class"":""java.util.Collections$EmptyMap""},
                            ""homePageUrl"":""http://localhost:8080/"",
                            ""statusPageUrl"":""http://localhost:8080/info"",
                            ""healthCheckUrl"":""http://localhost:8080/health"",
                            ""vipAddress"":""foo"",
                            ""isCoordinatingDiscoveryServer"":""false"",
                            ""lastUpdatedTimestamp"":""1458152330783"",
                            ""lastDirtyTimestamp"":""1458152330696"",
                            ""actionType"":""ADDED""
                        }]
                    }
                }";
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = JsonSerializer.Deserialize<JsonApplicationRoot>(json, options);
        Assert.NotNull(result);
        Assert.NotNull(result.Application);
        Assert.Equal("FOO", result.Application.Name);
        Assert.NotNull(result.Application.Instances);
        Assert.Equal(1, result.Application.Instances.Count);
    }
}