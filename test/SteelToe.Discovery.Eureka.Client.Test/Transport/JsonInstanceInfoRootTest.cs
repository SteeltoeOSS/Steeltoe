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

using SteelToe.Discovery.Eureka.AppInfo;
using SteelToe.Discovery.Eureka.Transport;
using System.IO;
using Xunit;

namespace SteelToe.Discovery.Eureka.Client.Test.Transport
{
    public class JsonInstanceInfoRootTest : AbstractBaseTest
    {
        [Fact]
        public void Deserialize_GoodJson()
        {
            var json = @"{ 
'instance':
    {
    'instanceId':'DESKTOP-GNQ5SUT',
    'app':'FOOBAR',
    'appGroupName':null,
    'ipAddr':'192.168.0.147',
    'sid':'na',
    'port':{'@enabled':true,'$':80},
    'securePort':{'@enabled':false,'$':443},
    'homePageUrl':'http://DESKTOP-GNQ5SUT:80/',
    'statusPageUrl':'http://DESKTOP-GNQ5SUT:80/Status',
    'healthCheckUrl':'http://DESKTOP-GNQ5SUT:80/healthcheck',
    'secureHealthCheckUrl':null,
    'vipAddress':'DESKTOP-GNQ5SUT:80',
    'secureVipAddress':'DESKTOP-GNQ5SUT:443',
    'countryId':1,
    'dataCenterInfo':{'@class':'com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo','name':'MyOwn'},
    'hostName':'DESKTOP-GNQ5SUT',
    'status':'UP',
    'overriddenstatus':'UNKNOWN',
    'leaseInfo':{'renewalIntervalInSecs':30,'durationInSecs':90,'registrationTimestamp':0,'lastRenewalTimestamp':0,'renewalTimestamp':0,'evictionTimestamp':0,'serviceUpTimestamp':0},
    'isCoordinatingDiscoveryServer':false,
    'metadata':{'@class':'java.util.Collections$EmptyMap','metadata':null},
    'lastUpdatedTimestamp':1458116137663,
    'lastDirtyTimestamp':1458116137663,
    'actionType':'ADDED',
    'asgName':null
    }
}";
            Stream stream = TestHelpers.StringToStream(json);
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