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

using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using System.IO;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Client.Test.Transport
{
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
            var stream = TestHelpers.StringToStream(json);
            var result = JsonApplicationRoot.Deserialize(stream);
            Assert.NotNull(result);
            Assert.NotNull(result.Application);
            Assert.Equal("FOO", result.Application.Name);
            Assert.NotNull(result.Application.Instances);
            Assert.Equal(1, result.Application.Instances.Count);
        }
    }
}
