// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Steeltoe.Connector.Services;
using System.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryOAuthConfigurerTest
{
    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var opts = new CloudFoundryOAuthOptions();
        CloudFoundryOAuthConfigurer.Configure(null, opts);

        var authURL = $"http://{CloudFoundryDefaults.OAuthServiceUrl}";
        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
        Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
        Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
        Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
        Assert.Equal(authURL + CloudFoundryDefaults.AuthorizationUri, opts.AuthorizationEndpoint);
        Assert.Equal(authURL + CloudFoundryDefaults.AccessTokenUri, opts.TokenEndpoint);
        Assert.Equal(authURL + CloudFoundryDefaults.UserInfoUri, opts.UserInformationEndpoint);
        Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
        Assert.True(opts.ValidateCertificates);
        Assert.Equal(6, opts.ClaimActions.Count());
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
        Assert.True(opts.SaveTokens);
        Assert.Null(opts.BackchannelHttpHandler);
    }

    [Fact]
    public void Configure_WithServiceInfo_ReturnsExpected()
    {
        var opts = new CloudFoundryOAuthOptions();
        var info = new SsoServiceInfo("foobar", "clientId", "secret", "http://domain");
        CloudFoundryOAuthConfigurer.Configure(info, opts);

        var authURL = "http://domain";
        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
        Assert.Equal("clientId", opts.ClientId);
        Assert.Equal("secret", opts.ClientSecret);
        Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
        Assert.Equal(authURL + CloudFoundryDefaults.AuthorizationUri, opts.AuthorizationEndpoint);
        Assert.Equal(authURL + CloudFoundryDefaults.AccessTokenUri, opts.TokenEndpoint);
        Assert.Equal(authURL + CloudFoundryDefaults.UserInfoUri, opts.UserInformationEndpoint);
        Assert.Equal(authURL + CloudFoundryDefaults.CheckTokenUri, opts.TokenInfoUrl);
        Assert.True(opts.ValidateCertificates);
        Assert.Equal(6, opts.ClaimActions.Count());
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
        Assert.True(opts.SaveTokens);
        Assert.Null(opts.BackchannelHttpHandler);
    }
}
