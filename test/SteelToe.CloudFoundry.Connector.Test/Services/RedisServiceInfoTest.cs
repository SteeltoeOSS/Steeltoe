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

using SteelToe.CloudFoundry.Connector.Services;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Test.Services
{
    public class RedisServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            string uri = "redis://joe:joes_password@localhost:1527/";
            RedisServiceInfo r1 = new RedisServiceInfo("myId", "localhost", 1527, "joes_password");
            RedisServiceInfo r2 = new RedisServiceInfo("myId", uri);

            Assert.Equal("myId", r1.Id);
            Assert.Equal("redis", r1.Scheme);
            Assert.Equal("localhost", r1.Host);
            Assert.Equal(1527, r1.Port);
            Assert.Equal("joes_password", r1.Password);
            Assert.Equal(null, r1.Path);
            Assert.Equal(null, r1.Query);


            Assert.Equal("myId", r2.Id);
            Assert.Equal("redis", r2.Scheme);
            Assert.Equal("localhost", r2.Host);
            Assert.Equal(1527, r2.Port);
            Assert.Equal("joe", r2.UserName);
            Assert.Equal("joes_password", r2.Password);
            Assert.Equal(string.Empty, r2.Path);
            Assert.Equal(null, r2.Query);
        }
    }
}
