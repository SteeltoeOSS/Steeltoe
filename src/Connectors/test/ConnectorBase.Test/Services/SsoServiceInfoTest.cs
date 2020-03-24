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

using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class SsoServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            var clientId = "clientId";
            var clientSecret = "clientSecret";
            var authDomain = "https://p-spring-cloud-services.uaa.my-cf.com/oauth/token";
            var r1 = new SsoServiceInfo("myId", clientId, clientSecret, authDomain);
            Assert.Equal("myId", r1.Id);
            Assert.Equal("clientId", r1.ClientId);
            Assert.Equal("clientSecret", r1.ClientSecret);
            Assert.Equal("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token", r1.AuthDomain);
        }
    }
}
