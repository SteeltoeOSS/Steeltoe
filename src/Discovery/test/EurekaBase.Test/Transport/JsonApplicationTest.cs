// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Test;
using System.IO;

using Xunit;

namespace Steeltoe.Discovery.Eureka.Transport.Test
{
    public class JsonApplicationTest : AbstractBaseTest
    {
        [Fact]
        public void Deserialize_GoodJson()
        {
            var json = @"
{
    ""name"":""FOO"",
    ""instance"":[{ 
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
    }]
}";
            var stream = TestHelpers.StringToStream(json);
            var result = JsonApplication.Deserialize(stream);
            Assert.NotNull(result);
            Assert.Equal("FOO", result.Name);
            Assert.NotNull(result.Instances);
            Assert.Equal(1, result.Instances.Count);

            // Rest is validated by JsonInstanceInfoTest
        }
    }
}
