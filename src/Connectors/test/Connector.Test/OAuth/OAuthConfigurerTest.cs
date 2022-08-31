// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.OAuth.Test;

public class OAuthConfigurerTest
{
    [Fact]
    public void Update_WithDefaultConnectorOptions_UpdatesOAuthOptions_AsExpected()
    {
        var serviceOptions = new OAuthServiceOptions();

        var options = new OAuthConnectorOptions
        {
            ValidateCertificates = false
        };

        var configurer = new OAuthConfigurer();
        configurer.UpdateOptions(options, serviceOptions);

        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultAccessTokenUri, serviceOptions.AccessTokenUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultClientId, serviceOptions.ClientId);
        Assert.Equal(OAuthConnectorDefaults.DefaultClientSecret, serviceOptions.ClientSecret);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultJwtTokenKey, serviceOptions.JwtKeyUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultCheckTokenUri, serviceOptions.TokenInfoUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultAuthorizationUri, serviceOptions.UserAuthorizationUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultUserInfoUri, serviceOptions.UserInfoUrl);
        Assert.False(serviceOptions.ValidateCertificates);
        Assert.NotNull(serviceOptions.Scope);
        Assert.Equal(0, serviceOptions.Scope.Count);
    }

    [Fact]
    public void Update_WithServiceInfo_UpdatesOAuthOptions_AsExpected()
    {
        var options = new OAuthServiceOptions();
        var si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "https://foo.bar");

        var configurer = new OAuthConfigurer();
        configurer.UpdateOptions(si, options);

        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultAccessTokenUri}", options.AccessTokenUrl);
        Assert.Equal("myClientId", options.ClientId);
        Assert.Equal("myClientSecret", options.ClientSecret);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultJwtTokenKey}", options.JwtKeyUrl);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultCheckTokenUri}", options.TokenInfoUrl);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultAuthorizationUri}", options.UserAuthorizationUrl);
        Assert.Equal($"https://foo.bar{OAuthConnectorDefaults.DefaultUserInfoUri}", options.UserInfoUrl);
        Assert.True(options.ValidateCertificates);
        Assert.NotNull(options.Scope);
        Assert.Equal(0, options.Scope.Count);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var options = new OAuthConnectorOptions();
        var configurer = new OAuthConfigurer();
        IOptions<OAuthServiceOptions> result = configurer.Configure(null, options);

        Assert.NotNull(result);
        OAuthServiceOptions opts = result.Value;
        Assert.NotNull(opts);

        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultAccessTokenUri, opts.AccessTokenUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultClientId, opts.ClientId);
        Assert.Equal(OAuthConnectorDefaults.DefaultClientSecret, opts.ClientSecret);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultJwtTokenKey, opts.JwtKeyUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultCheckTokenUri, opts.TokenInfoUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultAuthorizationUri, opts.UserAuthorizationUrl);
        Assert.Equal(OAuthConnectorDefaults.DefaultOAuthServiceUrl + OAuthConnectorDefaults.DefaultUserInfoUri, opts.UserInfoUrl);
        Assert.True(opts.ValidateCertificates);
        Assert.NotNull(opts.Scope);
        Assert.Equal(0, opts.Scope.Count);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var si = new SsoServiceInfo("myId", "myClientId", "myClientSecret", "https://foo.bar");
        var options = new OAuthConnectorOptions();
        var configurer = new OAuthConfigurer();
        IOptions<OAuthServiceOptions> result = configurer.Configure(si, options);

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
        Assert.True(opts.ValidateCertificates);
        Assert.NotNull(opts.Scope);
        Assert.Equal(0, opts.Scope.Count);
    }
}
