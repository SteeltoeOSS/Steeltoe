// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.Services;
using Xunit;

namespace Steeltoe.Connectors.CloudFoundry.Test.Services;

public sealed class SsoServiceInfoTest
{
    [Fact]
    public void Constructor_CreatesExpected()
    {
        const string clientId = "clientId";
        const string clientSecret = "clientSecret";
        const string authDomain = "https://p-spring-cloud-services.uaa.my-cf.com/oauth/token";
        var r1 = new SsoServiceInfo("myId", clientId, clientSecret, authDomain);
        Assert.Equal("myId", r1.Id);
        Assert.Equal("clientId", r1.ClientId);
        Assert.Equal("clientSecret", r1.ClientSecret);
        Assert.Equal("https://p-spring-cloud-services.uaa.my-cf.com/oauth/token", r1.AuthDomain);
    }
}
