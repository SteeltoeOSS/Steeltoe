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

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class EurekaServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            string uri = "https://username:password@hostname:1111/";
            string clientId = "clientId";
            string clientSecret = "clientSecret";
            string accessTokenUri = "https://p-spring-cloud-services.uaa.my-cf.com/oauth/token";
            EurekaServiceInfo r1 = new EurekaServiceInfo("myId", uri, clientId, clientSecret, accessTokenUri);

            Assert.Equal("myId", r1.Id);
            Assert.Equal("https", r1.Scheme);
            Assert.Equal("hostname", r1.Host);
            Assert.Equal(1111, r1.Port);
            Assert.Equal("password", r1.Password);
            Assert.Equal("username", r1.UserName);
            Assert.Equal("clientId", r1.ClientId);
            Assert.Equal("clientSecret", r1.ClientSecret);
            Assert.Equal("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token", r1.TokenUri);
            Assert.Equal("https://username:password@hostname:1111/", r1.Uri);
        }
    }
}
