// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Http.Test;

public sealed class HttpClientExtensionsTest
{
    [Fact]
    public void ConfigureForSteeltoe_sets_user_agent_and_timeout()
    {
        var httpClient = new HttpClient();

        httpClient.ConfigureForSteeltoe(TimeSpan.FromSeconds(5));

        httpClient.DefaultRequestHeaders.UserAgent.Should().HaveCount(1);
        httpClient.DefaultRequestHeaders.UserAgent.ElementAt(0).ToString().Should().Be(HttpClientExtensions.SteeltoeUserAgent);

        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAccessTokenAsync_with_username_and_password_sends_request_with_basic_auth()
    {
        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "https://auth-server.com/oauth/token").WithHeaders("Authorization", "Basic dGVzdC11c2VyOnRlc3QtcGFzc3dvcmQ=")
            .WithFormData("grant_type=client_credentials").Respond("application/json", "{ \"access_token\": \"secret\" }");

        var httpClient = new HttpClient(handler);

        string accessToken =
            await httpClient.GetAccessTokenAsync(new Uri("https://auth-server.com/oauth/token"), "test-user", "test-password", CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        httpClient.DefaultRequestHeaders.UserAgent.Should().HaveCount(1);
        httpClient.DefaultRequestHeaders.UserAgent.ElementAt(0).ToString().Should().Be(HttpClientExtensions.SteeltoeUserAgent);

        accessToken.Should().Be("secret");
    }

    [Fact]
    public async Task GetAccessTokenAsync_with_only_password_sends_request_with_basic_auth()
    {
        var handler = new DelegateToMockHttpClientHandler();

        handler.Mock.Expect(HttpMethod.Post, "https://auth-server.com/oauth/token").WithHeaders("Authorization", "Basic OnRlc3QtcGFzc3dvcmQ=")
            .WithFormData("grant_type=client_credentials").Respond("application/json", "{ \"access_token\": \"secret\" }");

        var httpClient = new HttpClient(handler);

        string accessToken =
            await httpClient.GetAccessTokenAsync(new Uri("https://auth-server.com/oauth/token"), null, "test-password", CancellationToken.None);

        handler.Mock.VerifyNoOutstandingExpectation();

        httpClient.DefaultRequestHeaders.UserAgent.Should().HaveCount(1);
        httpClient.DefaultRequestHeaders.UserAgent.ElementAt(0).ToString().Should().Be(HttpClientExtensions.SteeltoeUserAgent);

        accessToken.Should().Be("secret");
    }
}
