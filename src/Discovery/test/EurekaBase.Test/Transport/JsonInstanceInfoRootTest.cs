// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Client.Test.Transport
{
    public class JsonInstanceInfoRootTest : AbstractBaseTest
    {
        [Fact]
        public void Deserialize_GoodJson()
        {
            var json = @"
{ 
    ""instance"":
    {
        ""instanceId"":""DESKTOP-GNQ5SUT"",
        ""app"":""FOOBAR"",
        ""appGroupName"":null,
        ""ipAddr"":""192.168.0.147"",
        ""sid"":""na"",
        ""port"":{""@enabled"":true,""$"":80},
        ""securePort"":{""@enabled"":false,""$"":443},
        ""homePageUrl"":""http://DESKTOP-GNQ5SUT:80/"",
        ""statusPageUrl"":""http://DESKTOP-GNQ5SUT:80/Status"",
        ""healthCheckUrl"":""http://DESKTOP-GNQ5SUT:80/healthcheck"",
        ""secureHealthCheckUrl"":null,
        ""vipAddress"":""DESKTOP-GNQ5SUT:80"",
        ""secureVipAddress"":""DESKTOP-GNQ5SUT:443"",
        ""countryId"":1,
        ""dataCenterInfo"":{""@class"":""com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo"",""name"":""MyOwn""},
        ""hostName"":""DESKTOP-GNQ5SUT"",
        ""status"":""UP"",
        ""overriddenstatus"":""UNKNOWN"",
        ""leaseInfo"":{""renewalIntervalInSecs"":30,""durationInSecs"":90,""registrationTimestamp"":0,""lastRenewalTimestamp"":0,""renewalTimestamp"":0,""evictionTimestamp"":0,""serviceUpTimestamp"":0},
        ""isCoordinatingDiscoveryServer"":false,
        ""metadata"":{""@class"":""java.util.Collections$EmptyMap"",""metadata"":null},
        ""lastUpdatedTimestamp"":1458116137663,
        ""lastDirtyTimestamp"":1458116137663,
        ""actionType"":""ADDED"",
        ""asgName"":null
    }
}";
            var stream = TestHelpers.StringToStream(json);
            var result = JsonInstanceInfoRoot.Deserialize(stream);
            Assert.NotNull(result);
            Assert.NotNull(result.Instance);

            // Random check some values
            Assert.Equal(ActionType.ADDED, result.Instance.Actiontype);
            Assert.Equal("http://DESKTOP-GNQ5SUT:80/healthcheck", result.Instance.HealthCheckUrl);
            Assert.Equal("FOOBAR", result.Instance.AppName);
        }
    }
}
