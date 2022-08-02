// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.OAuth.Test;

public class OAuthConnectorFactoryTest
{
    [Fact]
    public void Create_ReturnsOAuthOptions()
    {
        var si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "https://foo.bar");
        var config = new OAuthConnectorOptions();

        var factory = new OAuthConnectorFactory(si, config);
        IOptions<OAuthServiceOptions> result = factory.Create(null);

        Assert.NotNull(result);
        OAuthServiceOptions opts = result.Value;
        Assert.NotNull(opts);

        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultAccessTokenUri}", opts.AccessTokenUrl);
        Assert.Equal("myClientId", opts.ClientId);
        Assert.Equal("myClientSecret", opts.ClientSecret);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultJwtTokenKey}", opts.JwtKeyUrl);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultCheckTokenUri}", opts.TokenInfoUrl);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultAuthorizationUri}", opts.UserAuthorizationUrl);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultUserInfoUri}", opts.UserInfoUrl);
        Assert.NotNull(opts.Scope);
        Assert.Equal(0, opts.Scope.Count);
    }
}
