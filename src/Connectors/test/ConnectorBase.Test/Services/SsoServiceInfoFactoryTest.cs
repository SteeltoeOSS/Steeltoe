// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Services
{
    public class SsoServiceInfoFactoryTest
    {
        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-identity",
                Tags = System.Array.Empty<string>(),
                Name = "mySSO",
                Plan = "sso",
                Credentials = new Credential()
                {
                    { "client_id", new Credential("clientId") },
                    { "client_secret", new Credential("clientSecret") },
                    { "auth_domain", new Credential("https://sso.login.system.testcloud.com") }
                }
            };
            var factory = new SsoServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsValidUAAServiceBinding()
        {
            var s = new Service()
            {
                Label = "user-provided",
                Tags = System.Array.Empty<string>(),
                Name = "mySSO",
                Credentials = new Credential()
                {
                    { "client_id", new Credential("clientId") },
                    { "client_secret", new Credential("clientSecret") },
                    { "uri", new Credential("uaa://sso.login.system.testcloud.com") }
                }
            };
            var factory = new SsoServiceInfoFactory();
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
            var factory = new SsoServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "p-identity",
                Tags = System.Array.Empty<string>(),
                Name = "mySSO",
                Plan = "sso",
                Credentials = new Credential()
                {
                    { "client_id", new Credential("clientId") },
                    { "client_secret", new Credential("clientSecret") },
                    { "auth_domain", new Credential("https://sso.login.system.testcloud.com") }
                }
            };
            var factory = new SsoServiceInfoFactory();
            var info = factory.Create(s) as SsoServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("mySSO", info.Id);
            Assert.Equal("clientId", info.ClientId);
            Assert.Equal("clientSecret", info.ClientSecret);
            Assert.Equal("https://sso.login.system.testcloud.com", info.AuthDomain);
        }

        [Fact]
        public void CreateWithURI_CreatesValidServiceBinding()
        {
            var s = new Service()
            {
                Label = "user-provided",
                Tags = System.Array.Empty<string>(),
                Name = "mySSO",
                Credentials = new Credential()
                {
                    { "client_id", new Credential("clientId") },
                    { "client_secret", new Credential("clientSecret") },
                    { "uri", new Credential("uaa://sso.login.system.testcloud.com") }
                }
            };
            var factory = new SsoServiceInfoFactory();
            var info = factory.Create(s) as SsoServiceInfo;
            Assert.NotNull(info);
            Assert.Equal("mySSO", info.Id);
            Assert.Equal("clientId", info.ClientId);
            Assert.Equal("clientSecret", info.ClientSecret);
            Assert.Equal("https://sso.login.system.testcloud.com", info.AuthDomain);
        }

        [Fact]
        public void UpdateUaaScheme_UpdatesSchemeProperly()
        {
            var uaa1 = "uaa://sso.login.system.testcloud.com";
            var factory = new SsoServiceInfoFactory();
            var result = factory.UpdateUaaScheme(uaa1);
            Assert.Equal("https://sso.login.system.testcloud.com", result);
            var uaa2 = "uaa://uaa.system.testcloud.com";
            result = factory.UpdateUaaScheme(uaa2);
            Assert.Equal("https://uaa.system.testcloud.com", result);
            var nonUaa = "https://uaa.system.testcloud.com";
            result = factory.UpdateUaaScheme(nonUaa);
            Assert.Equal(nonUaa, result);
        }
    }
}