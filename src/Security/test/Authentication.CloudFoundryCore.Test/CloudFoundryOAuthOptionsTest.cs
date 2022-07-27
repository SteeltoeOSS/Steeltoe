// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System.Linq;

using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryOAuthOptionsTest
{
    private const string DEFAULT_OAUTH_URL = "http://" + CloudFoundryDefaults.OAuthServiceUrl;
    private const string DEFAULT_ACCESSTOKEN_URL = DEFAULT_OAUTH_URL + CloudFoundryDefaults.AccessTokenUri;
    private const string DEFAULT_AUTHORIZATION_URL = DEFAULT_OAUTH_URL + CloudFoundryDefaults.AuthorizationUri;
    private const string DEFAULT_CHECKTOKEN_URL = DEFAULT_OAUTH_URL + CloudFoundryDefaults.CheckTokenUri;
    private const string DEFAULT_USERINFO_URL = DEFAULT_OAUTH_URL + CloudFoundryDefaults.UserInfoUri;

    public static TheoryData<string, string, string, string, string> SetEndpointsData()
    {
        var data = new TheoryData<string, string, string, string, string>();
        var newDomain = "http://not-the-original-domain";
        var newAccessTokenUrl = newDomain + CloudFoundryDefaults.AccessTokenUri;
        var newAuthorizationUrl = newDomain + CloudFoundryDefaults.AuthorizationUri;
        var newCheckTokenUrl = newDomain + CloudFoundryDefaults.CheckTokenUri;
        var newUserInfoUrl = newDomain + CloudFoundryDefaults.UserInfoUri;

        data.Add(string.Empty, default, default, default, default);
        data.Add("   ", default, default, default, default);
        data.Add(default, default, default, default, default);
        data.Add(newDomain, newAccessTokenUrl, newAuthorizationUrl, newCheckTokenUrl, newUserInfoUrl);

        return data;
    }

    [Fact]
    public void DefaultConstructor_SetsupDefaultOptions()
    {
        var opts = new CloudFoundryOAuthOptions();

        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
        Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
        Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
        Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
        Assert.Equal(DEFAULT_ACCESSTOKEN_URL, opts.TokenEndpoint);
        Assert.Equal(DEFAULT_AUTHORIZATION_URL, opts.AuthorizationEndpoint);
        Assert.Equal(DEFAULT_CHECKTOKEN_URL, opts.TokenInfoUrl);
        Assert.Equal(DEFAULT_USERINFO_URL, opts.UserInformationEndpoint);
        Assert.True(opts.ValidateCertificates);
        Assert.Equal(6, opts.ClaimActions.Count());
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, opts.SignInScheme);
        Assert.True(opts.SaveTokens);
    }

    [Theory]
    [MemberData(nameof(SetEndpointsData))]
    public void SetEndpoints_WithNewDomain_ReturnsExpected(
        string newDomain,
        string expectedAccessTokenUrl,
        string expectedAuthorizationUrl,
        string expectedCheckTokenUrl,
        string expectedUserInfoUrl)
    {
        var options = new CloudFoundryOAuthOptions();

        options.SetEndpoints(newDomain);

        Assert.Equal(expectedAccessTokenUrl ?? DEFAULT_ACCESSTOKEN_URL, options.TokenEndpoint);
        Assert.Equal(expectedAuthorizationUrl ?? DEFAULT_AUTHORIZATION_URL, options.AuthorizationEndpoint);
        Assert.Equal(expectedCheckTokenUrl ?? DEFAULT_CHECKTOKEN_URL, options.TokenInfoUrl);
        Assert.Equal(expectedUserInfoUrl ?? DEFAULT_USERINFO_URL, options.UserInformationEndpoint);
    }
}