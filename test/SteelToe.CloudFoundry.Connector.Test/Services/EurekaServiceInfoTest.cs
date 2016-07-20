using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SteelToe.CloudFoundry.Connector.Services.Test
{
    public class EurekaServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            string uri = "http://username:password@hostname:1111/";
            string clientId = "clientId";
            string clientSecret = "clientSecret";
            string accessTokeUri = "https://p-spring-cloud-services.uaa.my-cf.com/oauth/token";
            EurekaServiceInfo r1 = new EurekaServiceInfo("myId", uri, clientId, clientSecret, accessTokeUri);

            Assert.Equal("myId", r1.Id);
            Assert.Equal("http", r1.Scheme);
            Assert.Equal("hostname", r1.Host);
            Assert.Equal(1111, r1.Port);
            Assert.Equal("password", r1.Password);
            Assert.Equal("username", r1.UserName);
            Assert.Equal("clientId", r1.ClientId);
            Assert.Equal("clientSecret", r1.ClientSecret);
            Assert.Equal("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token", r1.TokenUri);
            Assert.Equal("http://username:password@hostname:1111/", r1.Uri);

        }
    }
}
