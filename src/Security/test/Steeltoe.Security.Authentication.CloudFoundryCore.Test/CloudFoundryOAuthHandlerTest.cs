// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
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
            var logger = new LoggerFactory().CreateLogger("ExchangeCodeAsync_SendsTokenRequest");
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

            var logger = new LoggerFactory().CreateLogger("ExchangeCodeAsync_SendsTokenRequest");

            AuthenticationProperties props = new AuthenticationProperties();
            string result = testHandler.TestBuildChallengeUrl(props, "http://foo.bar/redirect");
            Assert.Equal("http://Default_OAuthServiceUrl/oauth/authorize?response_type=code&client_id=Default_ClientId&redirect_uri=http%3A%2F%2Ffoo.bar%2Fredirect&scope=", result);
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

            var payload = JObject.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);

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

            var payload = JObject.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);

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

            var logger = new LoggerFactory().CreateLogger("CreateTicketAsync_SendsTokenRequest");

            ClaimsIdentity identity = new ClaimsIdentity();

            var payload = JObject.Parse(TestHelpers.GetValidTokenInfoRequestResponse());
            var tokens = OAuthTokenResponse.Success(payload);
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