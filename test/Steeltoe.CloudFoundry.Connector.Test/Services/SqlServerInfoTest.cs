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

using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class SqlServerInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            string uri = "jdbc:sqlserver://192.168.0.90:1433;databaseName=de5aa3a747c134b3d8780f8cc80be519e";
            SqlServerServiceInfo r1 = new SqlServerServiceInfo("myId", uri, "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt");

            Assert.Equal("myId", r1.Id);
            Assert.Equal("sqlserver", r1.Scheme);
            Assert.Equal("192.168.0.90", r1.Host);
            Assert.Equal(1433, r1.Port);
            Assert.Equal("7E1LxXnlH2hhlPVt", r1.Password);
            Assert.Equal("Dd6O1BPXUHdrmzbP", r1.UserName);
            Assert.Equal("databaseName=de5aa3a747c134b3d8780f8cc80be519e", r1.Path);
        }
    }
}
