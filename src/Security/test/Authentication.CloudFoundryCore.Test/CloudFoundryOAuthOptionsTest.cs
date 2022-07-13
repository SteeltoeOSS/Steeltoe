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
    private const string DefaultOauthUrl = $"http://{CloudFoundryDefaults.OAuthServiceUrl}";
    private const string DefaultAccessTokenUrl = DefaultOauthUrl + CloudFoundryDefaults.AccessTokenUri;
    private const string DefaultAuthorizationUrl = DefaultOauthUrl + CloudFoundryDefaults.AuthorizationUri;
    private const string DefaultCheckTokenUrl = DefaultOauthUrl + CloudFoundryDefaults.CheckTokenUri;
    private const string DefaultUserInfoUrl = DefaultOauthUrl + CloudFoundryDefaults.UserInfoUri;

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
    public void DefaultConstructor_SetsUpDefaultOptions()
    {
        var opts = new CloudFoundryOAuthOptions();

        Assert.Equal(CloudFoundryDefaults.AuthenticationScheme, opts.ClaimsIssuer);
        Assert.Equal(CloudFoundryDefaults.ClientId, opts.ClientId);
        Assert.Equal(CloudFoundryDefaults.ClientSecret, opts.ClientSecret);
        Assert.Equal(new PathString("/signin-cloudfoundry"), opts.CallbackPath);
        Assert.Equal(DefaultAccessTokenUrl, opts.TokenEndpoint);
        Assert.Equal(DefaultAuthorizationUrl, opts.AuthorizationEndpoint);
        Assert.Equal(DefaultCheckTokenUrl, opts.TokenInfoUrl);
        Assert.Equal(DefaultUserInfoUrl, opts.UserInformationEndpoint);
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

        Assert.Equal(expectedAccessTokenUrl ?? DefaultAccessTokenUrl, options.TokenEndpoint);
        Assert.Equal(expectedAuthorizationUrl ?? DefaultAuthorizationUrl, options.AuthorizationEndpoint);
        Assert.Equal(expectedCheckTokenUrl ?? DefaultCheckTokenUrl, options.TokenInfoUrl);
        Assert.Equal(expectedUserInfoUrl ?? DefaultUserInfoUrl, options.UserInformationEndpoint);
    }
}
