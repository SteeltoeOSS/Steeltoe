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

using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class UriServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            var uri = "mysql://joe:joes_password@localhost:1527/big_db";
            UriServiceInfo r1 = new TestUriServiceInfo("myId", "mysql", "localhost", 1527, "joe", "joes_password", "big_db");
            UriServiceInfo r2 = new TestUriServiceInfo("myId", uri);

            Assert.Equal("myId", r1.Id);
            Assert.Equal("mysql", r1.Scheme);
            Assert.Equal("localhost", r1.Host);
            Assert.Equal(1527, r1.Port);
            Assert.Equal("joe", r1.UserName);
            Assert.Equal("joes_password", r1.Password);
            Assert.Equal("big_db", r1.Path);
            Assert.Null(r1.Query);

            Assert.Equal("myId", r2.Id);
            Assert.Equal("mysql", r2.Scheme);
            Assert.Equal("localhost", r2.Host);
            Assert.Equal(1527, r2.Port);
            Assert.Equal("joe", r2.UserName);
            Assert.Equal("joes_password", r2.Password);
            Assert.Equal("big_db", r2.Path);
            Assert.Null(r2.Query);
        }
    }
}
