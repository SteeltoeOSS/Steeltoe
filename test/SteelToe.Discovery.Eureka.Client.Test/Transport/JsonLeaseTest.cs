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

using SteelToe.Discovery.Eureka.Client.Test;
using System.IO;
using Xunit;

namespace SteelToe.Discovery.Eureka.Transport.Test
{
    public class JsonLeaseTest : AbstractBaseTest
    {
        [Fact]
        public void Deserialize_GoodJson()
        {
            var json = @"
{   
    'renewalIntervalInSecs':30,
    'durationInSecs':90,
    'registrationTimestamp':1457714988223,
    'lastRenewalTimestamp':1457716158319,
    'evictionTimestamp':0,
    'serviceUpTimestamp':1457714988223
}";
            Stream stream = TestHelpers.StringToStream(json);
            var leaseInfo = JsonLeaseInfo.Deserialize(stream);
            Assert.NotNull(leaseInfo);
            Assert.Equal(30, leaseInfo.RenewalIntervalInSecs);
            Assert.Equal(90, leaseInfo.DurationInSecs);
            Assert.Equal(1457714988223, leaseInfo.RegistrationTimestamp);
            Assert.Equal(1457716158319, leaseInfo.LastRenewalTimestamp);
            Assert.Equal(0, leaseInfo.EvictionTimestamp);
            Assert.Equal(1457714988223, leaseInfo.ServiceUpTimestamp);
        }

    }
}
