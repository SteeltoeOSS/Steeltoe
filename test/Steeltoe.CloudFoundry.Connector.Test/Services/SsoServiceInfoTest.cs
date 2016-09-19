using Steeltoe.CloudFoundry.Connector.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Test.Services
{
    public class SsoServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            string clientId = "clientId";
            string clientSecret = "clientSecret";
            string authDomain = "https://p-spring-cloud-services.uaa.my-cf.com/oauth/token";
            SsoServiceInfo r1 = new SsoServiceInfo("myId", clientId, clientSecret, authDomain);
            Assert.Equal("myId", r1.Id);
            Assert.Equal("clientId", r1.ClientId);
            Assert.Equal("clientSecret", r1.ClientSecret);
            Assert.Equal("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token", r1.AuthDomain);

        }
    }
}
