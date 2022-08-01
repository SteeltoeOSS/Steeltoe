// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Connector.OAuth.Test;

public class OAuthConnectorOptionsTest
{
    [Fact]
    public void Constructor_ThrowsIfConfigNull()
    {
        const IConfiguration config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new OAuthConnectorOptions(config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void Constructor_BindsValues()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["security:oauth2:client:oauthServiceUrl"] = "https://foo.bar",
            ["security:oauth2:client:clientid"] = "clientid",
            ["security:oauth2:client:clientSecret"] = "clientsecret",
            ["security:oauth2:client:userAuthorizationUri"] = "userauthorizationuri",
            ["security:oauth2:client:accessTokenUri"] = "accesstokenuri",
            ["security:oauth2:client:scope:0"] = "foo",
            ["security:oauth2:client:scope:1"] = "bar",
            ["security:oauth2:resource:userInfoUri"] = "userinfouri",
            ["security:oauth2:resource:tokenInfoUri"] = "tokeninfouri",
            ["security:oauth2:resource:jwtKeyUri"] = "jwtkeyuri"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new OAuthConnectorOptions(config);
        Assert.Equal("accesstokenuri", options.AccessTokenUri);
        Assert.Equal("clientid", options.ClientId);
        Assert.Equal("clientsecret", options.ClientSecret);
        Assert.Equal("jwtkeyuri", options.JwtKeyUri);
        Assert.Equal("https://foo.bar", options.OAuthServiceUrl);
        Assert.Equal("tokeninfouri", options.TokenInfoUri);
        Assert.Equal("userauthorizationuri", options.UserAuthorizationUri);
        Assert.Equal("userinfouri", options.UserInfoUri);
        Assert.NotNull(options.Scope);
        Assert.Equal(2, options.Scope.Count);
        Assert.True(options.Scope.Contains("foo") && options.Scope.Contains("bar"));
        Assert.True(options.ValidateCertificates);
    }

    [Fact]
    public void ValidateCertificates_Binds()
    {
        // arrange a configuration with validateCertificates=false
        var appsettings = new Dictionary<string, string>
        {
            ["security:oauth2:client:validateCertificates"] = "false",
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new OAuthConnectorOptions(config);

        Assert.False(options.ValidateCertificates);
    }

    [Fact]
    public void Validate_Certificates_Binds()
    {
        // arrange a configuration with validateCertificates=false
        var appsettings = new Dictionary<string, string>
        {
            ["security:oauth2:client:validate_certificates"] = "false",
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();

        var options = new OAuthConnectorOptions(config);

        Assert.False(options.ValidateCertificates);
    }
}
