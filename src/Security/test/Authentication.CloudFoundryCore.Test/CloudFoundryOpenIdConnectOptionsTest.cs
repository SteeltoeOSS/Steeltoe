// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryOpenIdConnectOptionsTest
{
    [Fact]
    public void DefaultConstructor_SetsDefaultOptions()
    {
        var opts = new CloudFoundryOpenIdConnectOptions();

        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
        Assert.Equal("https://" + CloudFoundryDefaults.OAuthServiceUrl, opts.Authority);
        Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
        Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
        Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
        Assert.True(opts.ValidateCertificates);
        Assert.Equal(19, opts.ClaimActions.Count());
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
        Assert.False(opts.SaveTokens);
    }
}