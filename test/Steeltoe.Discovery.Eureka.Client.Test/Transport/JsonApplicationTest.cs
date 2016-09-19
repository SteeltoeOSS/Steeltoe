//
// Copyright 2015 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Steeltoe.Discovery.Eureka.Client.Test;
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
'name':'FOO',
'instance':[{ 
    'instanceId':'localhost:foo',
    'hostName':'localhost',
    'app':'FOO',
    'ipAddr':'192.168.56.1',
    'status':'UP',
    'overriddenstatus':'UNKNOWN',
    'port':{'$':8080,'@enabled':'true'},
    'securePort':{'$':443,'@enabled':'false'},
    'countryId':1,
    'dataCenterInfo':{'@class':'com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo','name':'MyOwn'},
    'leaseInfo':{'renewalIntervalInSecs':30,'durationInSecs':90,'registrationTimestamp':1457714988223,'lastRenewalTimestamp':1457716158319,'evictionTimestamp':0,'serviceUpTimestamp':1457714988223},
    'metadata':{'@class':'java.util.Collections$EmptyMap'},
    'homePageUrl':'http://localhost:8080/',
    'statusPageUrl':'http://localhost:8080/info',
    'healthCheckUrl':'http://localhost:8080/health',
    'vipAddress':'foo',
    'isCoordinatingDiscoveryServer':'false',
    'lastUpdatedTimestamp':'1457714988223',
    'lastDirtyTimestamp':'1457714988172',
    'actionType':'ADDED'
    }]
}";
            Stream stream = TestHelpers.StringToStream(json);
            var result = JsonApplication.Deserialize(stream);
            Assert.NotNull(result);
            Assert.Equal("FOO", result.Name);
            Assert.NotNull(result.Instances);
            Assert.Equal(1, result.Instances.Count);
            // Rest is validated by JsonInstanceInfoTest
        }
    }
}
