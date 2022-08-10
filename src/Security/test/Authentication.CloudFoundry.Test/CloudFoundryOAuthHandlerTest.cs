// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryOAuthHandlerTest
{
    [Fact]
    public async Task ExchangeCodeAsync_SendsTokenRequest_ReturnsValidTokenInfo()
    {
        var handler = new TestMessageHandler();

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TestHelpers.GetValidTokenRequestResponse())
        };

        handler.Response = response;

        var client = new HttpClient(handler);

        var opts = new CloudFoundryOAuthOptions
        {
            Backchannel = client
        };

        MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);
        OAuthTokenResponse resp = await testHandler.TestExchangeCodeAsync("code", "redirectUri");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal(opts.TokenEndpoint.ToLowerInvariant(), handler.LastRequest.RequestUri.ToString().ToLowerInvariant());

        Assert.NotNull(resp);
        Assert.NotNull(resp.Response);
        Assert.Equal("bearer", resp.TokenType);
        Assert.NotNull(resp.AccessToken);
        Assert.NotNull(resp.RefreshToken);
    }

    [Fact]
    public async Task ExchangeCodeAsync_SendsTokenRequest_ReturnsErrorResponse()
    {
        var handler = new TestMessageHandler();

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(string.Empty)
        };

        handler.Response = response;

        var client = new HttpClient(handler);

        var opts = new CloudFoundryOAuthOptions
        {
            Backchannel = client
        };

        MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);
        OAuthTokenResponse resp = await testHandler.TestExchangeCodeAsync("code", "http://redirectUri");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal(opts.TokenEndpoint.ToLowerInvariant(), handler.LastRequest.RequestUri.ToString().ToLowerInvariant());

        Assert.NotNull(resp);
        Assert.NotNull(resp.Error);
        Assert.Contains("OAuth token endpoint failure", resp.Error.Message);
    }

    [Fact]
    public void BuildChallengeUrl_CreatesCorrectUrl()
    {
        var handler = new TestMessageHandler();

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TestHelpers.GetValidTokenRequestResponse())
        };

        handler.Response = response;

        var client = new HttpClient(handler);

        var opts = new CloudFoundryOAuthOptions
        {
            Backchannel = client
        };

        MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

        var props = new AuthenticationProperties();
        string result = testHandler.TestBuildChallengeUrl(props, "https://foo.bar/redirect");

        Assert.Equal(
            "http://Default_OAuthServiceUrl/oauth/authorize?response_type=code&client_id=Default_ClientId&redirect_uri=https%3A%2F%2Ffoo.bar%2Fredirect&scope=",
            result);
    }

    [Fact]
    public void GetTokenInfoRequestParameters_ReturnsCorrectly()
    {
        var client = new HttpClient(new TestMessageHandler());

        var opts = new CloudFoundryOAuthOptions
        {
            Backchannel = client
        };

        MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

        JsonDocument payload = JsonDocument.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
        OAuthTokenResponse tokens = OAuthTokenResponse.Success(payload);
        Dictionary<string, string> parameters = testHandler.GetTokenInfoRequestParameters(tokens);
        Assert.NotNull(parameters);

        Assert.Equal(parameters["token"], tokens.AccessToken);
    }

    [Fact]
    public void GetTokenInfoRequestMessage_ReturnsCorrectly()
    {
        var client = new HttpClient(new TestMessageHandler());

        var opts = new CloudFoundryOAuthOptions
        {
            Backchannel = client
        };

        MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

        JsonDocument payload = JsonDocument.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
        OAuthTokenResponse tokens = OAuthTokenResponse.Success(payload);

        HttpRequestMessage message = testHandler.GetTokenInfoRequestMessage(tokens);
        Assert.NotNull(message);
        var content = message.Content as FormUrlEncodedContent;
        Assert.NotNull(content);
        Assert.Equal(HttpMethod.Post, message.Method);

        Assert.Contains(new MediaTypeWithQualityHeaderValue("application/json"), message.Headers.Accept);
    }

    [Fact]
    public async Task CreateTicketAsync_SendsTokenInfoRequest_ReturnsValidTokenInfo()
    {
        var handler = new TestMessageHandler();

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(TestHelpers.GetValidTokenInfoRequestResponse())
        };

        handler.Response = response;

        var client = new HttpClient(handler);

        var opts = new CloudFoundryOAuthOptions
        {
            Backchannel = client
        };

        MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

        var identity = new ClaimsIdentity();

        JsonDocument payload = JsonDocument.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
        OAuthTokenResponse tokens = OAuthTokenResponse.Success(payload);
        await testHandler.TestCreateTicketAsync(identity, new AuthenticationProperties(), tokens);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal(opts.TokenInfoUrl.ToLowerInvariant(), handler.LastRequest.RequestUri.ToString().ToLowerInvariant());

        Assert.Equal("testssouser", identity.Name);
        Assert.Equal(4, identity.Claims.Count());
        identity.HasClaim(ClaimTypes.Email, "testssouser@testcloud.com");
        identity.HasClaim(ClaimTypes.NameIdentifier, "13bb6841-e4d6-4a9a-876c-9ef13aa61cc7");
        identity.HasClaim(ClaimTypes.Name, "testssouser");
        identity.HasClaim("openid", string.Empty);
    }

    private MyTestCloudFoundryHandler GetTestHandler(CloudFoundryOAuthOptions options)
    {
        var loggerFactory = new LoggerFactory();
        IOptionsMonitor<CloudFoundryOAuthOptions> monitor = new MonitorWrapper<CloudFoundryOAuthOptions>(options);
        var encoder = UrlEncoder.Default;
        var clock = new TestClock();
        var testHandler = new MyTestCloudFoundryHandler(monitor, loggerFactory, encoder, clock);

        testHandler.InitializeAsync(
            new AuthenticationScheme(CloudFoundryDefaults.AuthenticationScheme, CloudFoundryDefaults.AuthenticationScheme, typeof(CloudFoundryOAuthHandler)),
            new DefaultHttpContext()).Wait();

        return testHandler;
    }
}
