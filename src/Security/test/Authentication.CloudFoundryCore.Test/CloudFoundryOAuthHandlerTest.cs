// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
#if !NETCOREAPP3_1
using Newtonsoft.Json.Linq;
#endif
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
#if NETCOREAPP3_1
using System.Text.Json;
#endif
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryOAuthHandlerTest
    {
        [Fact]
        public async void ExchangeCodeAsync_SendsTokenRequest_ReturnsValidTokenInfo()
        {
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(TestHelpers.GetValidTokenRequestResponse())
            };
            handler.Response = response;

            HttpClient client = new HttpClient(handler);

            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };

            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);
            var resp = await testHandler.TestExchangeCodeAsync("code", "redirectUri");

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
        public async void ExchangeCodeAsync_SendsTokenRequest_ReturnsErrorResponse()
        {
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
            {
                Content = new StringContent(string.Empty)
            };
            handler.Response = response;

            HttpClient client = new HttpClient(handler);
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };

            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);
            var resp = await testHandler.TestExchangeCodeAsync("code", "http://redirectUri");

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
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(TestHelpers.GetValidTokenRequestResponse())
            };
            handler.Response = response;

            HttpClient client = new HttpClient(handler);

            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };
            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

            AuthenticationProperties props = new AuthenticationProperties();
            string result = testHandler.TestBuildChallengeUrl(props, "https://foo.bar/redirect");
            Assert.Equal("http://Default_OAuthServiceUrl/oauth/authorize?response_type=code&client_id=Default_ClientId&redirect_uri=https%3A%2F%2Ffoo.bar%2Fredirect&scope=", result);
        }

        [Fact]
        public void GetTokenInfoRequestParameters_ReturnsCorrectly()
        {
            HttpClient client = new HttpClient(new TestMessageHandler());
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };

            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

#if NETCOREAPP3_1
            var payload = JsonDocument.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
#else
            var payload = JObject.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
#endif
            var parameters = testHandler.GetTokenInfoRequestParameters(tokens);
            Assert.NotNull(parameters);

            Assert.Equal(parameters["token"], tokens.AccessToken);
        }

        [Fact]
        public void GetTokenInfoRequestMessage_ReturnsCorrectly()
        {
            HttpClient client = new HttpClient(new TestMessageHandler());
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };
            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

#if NETCOREAPP3_1
            var payload = JsonDocument.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
#else
            var payload = JObject.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
#endif

            var message = testHandler.GetTokenInfoRequestMessage(tokens);
            Assert.NotNull(message);
            var content = message.Content as FormUrlEncodedContent;
            Assert.NotNull(content);
            Assert.Equal(HttpMethod.Post, message.Method);

            message.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [Fact]
        public async void CreateTicketAsync_SendsTokenInfoRequest_ReturnsValidTokenInfo()
        {
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(TestHelpers.GetValidTokenInfoRequestResponse())
            };
            handler.Response = response;

            HttpClient client = new HttpClient(handler);
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };
            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

            ClaimsIdentity identity = new ClaimsIdentity();

#if NETCOREAPP3_1
            var payload = JsonDocument.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
#else
            var payload = JObject.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
#endif
            var resp = await testHandler.TestCreateTicketAsync(identity, new AuthenticationProperties(), tokens);

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
            LoggerFactory loggerFactory = new LoggerFactory();
            IOptionsMonitor<CloudFoundryOAuthOptions> monitor = new MonitorWrapper<CloudFoundryOAuthOptions>(options);
            UrlEncoder encoder = UrlEncoder.Default;
            TestClock clock = new TestClock();
            MyTestCloudFoundryHandler testHandler = new MyTestCloudFoundryHandler(monitor, loggerFactory, encoder, clock);
            testHandler.InitializeAsync(
                 new AuthenticationScheme(CloudFoundryDefaults.AuthenticationScheme, CloudFoundryDefaults.AuthenticationScheme, typeof(CloudFoundryOAuthHandler)),
                 new DefaultHttpContext()).Wait();
            return testHandler;
        }
    }
}