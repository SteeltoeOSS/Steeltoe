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

using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class MongoServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            string uri = "mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355";
            MongoServiceInfo r1 = new MongoServiceInfo("myId", uri);
            MongoServiceInfo r2 = new MongoServiceInfo("myId", "192.168.0.90", 3306, "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt", "cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

            Assert.Equal("myId", r1.Id);
            Assert.Equal("mongodb", r1.Scheme);
            Assert.Equal("192.168.0.90", r1.Host);
            Assert.Equal(3306, r1.Port);
            Assert.Equal("7E1LxXnlH2hhlPVt", r1.Password);
            Assert.Equal("Dd6O1BPXUHdrmzbP", r1.UserName);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", r1.Path);
            Assert.Null(r1.Query);

            Assert.Equal("myId", r2.Id);
            Assert.Equal("mongodb", r2.Scheme);
            Assert.Equal("192.168.0.90", r2.Host);
            Assert.Equal(3306, r2.Port);
            Assert.Equal("7E1LxXnlH2hhlPVt", r2.Password);
            Assert.Equal("Dd6O1BPXUHdrmzbP", r2.UserName);
            Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", r2.Path);
            Assert.Null(r2.Query);
        }
    }
}
