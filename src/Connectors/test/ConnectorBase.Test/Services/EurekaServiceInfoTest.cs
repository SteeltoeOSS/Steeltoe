// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class EurekaServiceInfoTest
    {
        [Fact]
        public void Constructor_CreatesExpected()
        {
            var uri = "https://username:password@hostname:1111/";
            var clientId = "clientId";
            var clientSecret = "clientSecret";
            var accessTokenUri = "https://p-spring-cloud-services.uaa.my-cf.com/oauth/token";
            var r1 = new EurekaServiceInfo("myId", uri, clientId, clientSecret, accessTokenUri);

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
