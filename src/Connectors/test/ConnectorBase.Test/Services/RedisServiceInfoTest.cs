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

using Steeltoe.CloudFoundry.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Services
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
            Assert.Null(r1.Path);
            Assert.Null(r1.Query);

            Assert.Equal("myId", r2.Id);
            Assert.Equal("redis", r2.Scheme);
            Assert.Equal("localhost", r2.Host);
            Assert.Equal(1527, r2.Port);
            Assert.Equal("joe", r2.UserName);
            Assert.Equal("joes_password", r2.Password);
            Assert.Equal(string.Empty, r2.Path);
            Assert.Null(r2.Query);
        }

        [Fact]
        public void Constructor_CreatesExpected_withSecure()
        {
            string uri = "rediss://:joes_password@localhost:6380/";
            RedisServiceInfo r1 = new RedisServiceInfo("myId", "localhost", 1527, "joes_password");
            RedisServiceInfo r2 = new RedisServiceInfo("myId", uri);

            Assert.Equal("myId", r1.Id);
            Assert.Equal("redis", r1.Scheme);
            Assert.Equal("localhost", r1.Host);
            Assert.Equal(1527, r1.Port);
            Assert.Equal("joes_password", r1.Password);
            Assert.Null(r1.Path);
            Assert.Null(r1.Query);

            Assert.Equal("myId", r2.Id);
            Assert.Equal("rediss", r2.Scheme);
            Assert.Equal("localhost", r2.Host);
            Assert.Equal(6380, r2.Port);
            Assert.Equal("joes_password", r2.Password);
            Assert.Equal(string.Empty, r2.Path);
            Assert.Null(r2.Query);
        }

        [Theory]
        [InlineData("redis")]
        [InlineData("rediss")]
        public void Constructor_CreatesExpected_WithSchema(string scheme)
        {
            string uri = $"{scheme}://:joes_password@localhost:6380/";
            RedisServiceInfo redisInfo = new RedisServiceInfo("myId", scheme, "localhost", 1527, "joes_password");

            Assert.Equal("myId", redisInfo.Id);
            Assert.Equal(scheme, redisInfo.Scheme);
            Assert.Equal("localhost", redisInfo.Host);
            Assert.Equal(1527, redisInfo.Port);
            Assert.Equal("joes_password", redisInfo.Password);
            Assert.Null(redisInfo.Path);
            Assert.Null(redisInfo.Query);
        }
    }
}
