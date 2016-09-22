using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Services
{
    public class SsoServiceInfoFactoryTest
    {
        [Fact]
        public void Accept_AcceptsValidServiceBinding()
        {
            Service s = new Service()
            {
                Label = "p-identity",
                Tags = new string[0],
                Name = "mySSO",
                Plan = "sso",
                Credentials = new Credential() {
                    { "client_id", new Credential("clientId")},
                    { "client_secret", new Credential("clientSecret")},
                    { "auth_domain", new Credential("https://sso.login.system.testcloud.com")}
                    }
            };
            SsoServiceInfoFactory factory = new SsoServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_AcceptsValidUAAServiceBinding()
        {
            Service s = new Service()
            {
                Label = "user-provided",
                Tags = new string[0],
                Name = "mySSO",
                Credentials = new Credential() {
                    { "client_id", new Credential("clientId")},
                    { "client_secret", new Credential("clientSecret")},
                    { "uri", new Credential("uaa://sso.login.system.testcloud.com") }
                    }
            };
            SsoServiceInfoFactory factory = new SsoServiceInfoFactory();
            Assert.True(factory.Accept(s));
        }

        [Fact]
        public void Accept_RejectsInvalidServiceBinding()
        {
            Service s = new Service()
            {
                Label = "p-mysql",
                Tags = new string[] { "foobar", "relational" },
                Name = "mySqlService",
                Plan = "100mb-dev",
                Credentials = new Credential() {
                    { "hostname", new Credential("192.168.0.90")},
                    { "port", new Credential("3306")},
                    { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355")},
                    { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                    { "password", new Credential("7E1LxXnlH2hhlPVt") },
                    { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                    { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
                    }
            };
            SsoServiceInfoFactory factory = new SsoServiceInfoFactory();
            Assert.False(factory.Accept(s));
        }

        [Fact]
        public void Create_CreatesValidServiceBinding()
        {
            Service s = new Service()
            {
                Label = "p-identity",
                Tags = new string[0],
                Name = "mySSO",
                Plan = "sso",
                Credentials = new Credential() {
                    { "client_id", new Credential("clientId")},
                    { "client_secret", new Credential("clientSecret")},
                    { "auth_domain", new Credential("https://sso.login.system.testcloud.com")}
                    }
            };
            SsoServiceInfoFactory factory = new SsoServiceInfoFactory();
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
            Service s = new Service()
            {
                Label = "user-provided",
                Tags = new string[0],
                Name = "mySSO",
                Credentials = new Credential() {
                    { "client_id", new Credential("clientId")},
                    { "client_secret", new Credential("clientSecret")},
                    { "uri", new Credential("uaa://sso.login.system.testcloud.com") }
                    }
            };
            SsoServiceInfoFactory factory = new SsoServiceInfoFactory();
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
            string uaa1 = "uaa://sso.login.system.testcloud.com";
            SsoServiceInfoFactory factory = new SsoServiceInfoFactory();
            string result = factory.UpdateUaaScheme(uaa1);
            Assert.Equal("https://sso.login.system.testcloud.com", result);
            string uaa2 = "uaa://uaa.system.testcloud.com";
            result = factory.UpdateUaaScheme(uaa2);
            Assert.Equal("https://uaa.system.testcloud.com", result);
            string nonUaa = "https://uaa.system.testcloud.com";
            result = factory.UpdateUaaScheme(nonUaa);
            Assert.Equal(nonUaa, result);
        }
    }
}