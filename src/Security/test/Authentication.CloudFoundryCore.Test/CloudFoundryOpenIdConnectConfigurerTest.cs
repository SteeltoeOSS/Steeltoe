// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryOpenIdConnectConfigurerTest
{
    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var connectOptions = new OpenIdConnectOptions();

        CloudFoundryOpenIdConnectConfigurer.Configure(null, connectOptions, new CloudFoundryOpenIdConnectOptions
        {
            ValidateCertificates = false
        });

        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, connectOptions.ClaimsIssuer);
        Assert.Equal(CloudFoundryDefaults.ClientId, connectOptions.ClientId);
        Assert.Equal(CloudFoundryDefaults.ClientSecret, connectOptions.ClientSecret);
        Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), connectOptions.CallbackPath);
        Assert.Equal(19, connectOptions.ClaimActions.Count());
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, connectOptions.SignInScheme);
        Assert.False(connectOptions.SaveTokens);
        Assert.NotNull(connectOptions.BackchannelHttpHandler);
    }

    [Fact]
    public void Configure_WithServiceInfo_ReturnsExpected()
    {
        string authUrl = "https://domain";
        var connectOptions = new OpenIdConnectOptions();
        var info = new SsoServiceInfo("foobar", "clientId", "secret", authUrl);

        CloudFoundryOpenIdConnectConfigurer.Configure(info, connectOptions, new CloudFoundryOpenIdConnectOptions());

        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, connectOptions.ClaimsIssuer);
        Assert.Equal(authUrl, connectOptions.Authority);
        Assert.Equal("clientId", connectOptions.ClientId);
        Assert.Equal("secret", connectOptions.ClientSecret);
        Assert.Equal(new PathString(CloudFoundryDefaults.CallbackPath), connectOptions.CallbackPath);
        Assert.Null(connectOptions.BackchannelHttpHandler);
        Assert.Equal(19, connectOptions.ClaimActions.Count());
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, connectOptions.SignInScheme);
        Assert.False(connectOptions.SaveTokens);
        Assert.Null(connectOptions.BackchannelHttpHandler);
    }
}
