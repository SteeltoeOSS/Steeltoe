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
using Steeltoe.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class EurekaServiceInfoFactoryTest
    {
        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-eureka",
                Tags = new string[] { "eureka", "discovery", "registry", "spring-cloud" },
                Name = "eurekaService",
                Plan = "standard",
                Credentials = new Credential()
                {
                    { "client_id", new Credential("clientId") },
                    { "client_secret", new Credential("clientSecret") },
                    { "access_token_uri", new Credential("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token") },
                    { "uri", new Credential("https://username:password@192.168.0.90:1111/") },
                }
            };
            var factory = new EurekaServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsInvalidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-mysql",
                Tags = new string[] { "foobar", "relational" },
                Name = "mySqlService",
                Plan = "100mb-dev",
                Credentials = new Credential()
                {
                    { "hostname", new Credential("192.168.0.90") },
                    { "port", new Credential("3306") },
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                }
            };
            var factory = new EurekaServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-eureka",
                Tags = new string[] { "eureka", "discovery", "registry", "spring-cloud" },
                Name = "eurekaService",
                Plan = "standard",
                Credentials = new Credential()
                {
                    { "client_id", new Credential("clientId") },
                    { "client_secret", new Credential("clientSecret") },
                    { "access_token_uri", new Credential("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token") },
                    { "uri", new Credential("https://username:password@192.168.0.90:1111/") },
                }
            };
            var factory = new EurekaServiceInfoFactory();
            var info = factory.Create(s) as EurekaServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("eurekaService", info.Id);
            Assert.Equal("password", info.Password);
            Assert.Equal("username", info.UserName);
            Assert.Equal("192.168.0.90", info.Host);
            Assert.Equal(1111, info.Port);
            Assert.Equal("clientId", info.ClientId);
            Assert.Equal("clientSecret", info.ClientSecret);
            Assert.Equal("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token", info.TokenUri);
            Assert.Equal("https://username:password@192.168.0.90:1111/", info.Uri);
        }
    }
}
