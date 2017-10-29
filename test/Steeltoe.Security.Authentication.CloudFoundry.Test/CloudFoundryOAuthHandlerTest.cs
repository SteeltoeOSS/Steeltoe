//
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
//

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OAuth;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Text.Encodings.Web;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.Features;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{

    public class CloudFoundryOAuthHandlerTest
    {

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

        [Fact]
        public void GetTokenRequestParameters_ReturnsCorrectly()
        {
   
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = new HttpClient(new TestMessageHandler())
            };
     
            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

            var parameters = testHandler.GetTokenRequestParameters("code", "redirectUri");
            Assert.NotNull(parameters);


            Assert.Equal(parameters["client_id"], opts.ClientId);
            Assert.Equal( "redirectUri", parameters["redirect_uri"]);
            Assert.Equal(parameters["client_secret"], opts.ClientSecret);
            Assert.Equal( "code", parameters["code"]);
            Assert.Equal( "authorization_code",parameters["grant_type"]);

        }

        [Fact]
        public void GetTokenRequestMessage_ReturnsCorrectly()
        {
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = new HttpClient(new TestMessageHandler())
            };

            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);

            var message = testHandler.GetTokenRequestMessage("code", "redirectUri");
            Assert.NotNull(message);
            var content = message.Content as FormUrlEncodedContent;
            Assert.NotNull(content);
            Assert.Equal(HttpMethod.Post, message.Method);

            message.Headers.Accept.Contains(new MediaTypeWithQualityHeaderValue("application/json"));

        }

        [Fact]
        public async void ExchangeCodeAsync_SendsTokenRequest_ReturnsValidTokenInfo()
        {
            TestMessageHandler handler = new TestMessageHandler();
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(TestHelpers.GetValidTokenRequestResponse());
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
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            response.Content = new StringContent("");
            handler.Response = response;

            HttpClient client = new HttpClient(handler);
            var opts = new CloudFoundryOAuthOptions()
            {
                Backchannel = client
            };

            MyTestCloudFoundryHandler testHandler = GetTestHandler(opts);
            var logger = new LoggerFactory().CreateLogger("ExchangeCodeAsync_SendsTokenRequest");
            var resp = await testHandler.TestExchangeCodeAsync("code", "redirectUri");

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
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(TestHelpers.GetValidTokenRequestResponse());
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
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(TestHelpers.GetValidTokenInfoRequestResponse());
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


    }

    class MyTestCloudFoundryHandler : CloudFoundryOAuthHandler
    {
        public MyTestCloudFoundryHandler(
            IOptionsMonitor<CloudFoundryOAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        public async Task<AuthenticationTicket> TestCreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            return await base.CreateTicketAsync(identity, properties, tokens);
        }
        public async Task<OAuthTokenResponse> TestExchangeCodeAsync(string code, string redirectUri)
        {
            return await base.ExchangeCodeAsync(code, redirectUri);
        }

        public string TestBuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            return base.BuildChallengeUrl(properties, redirectUri);
        }
    }
    class TestResponse : IHttpResponseFeature
    {
        public Stream Body
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool HasStarted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IHeaderDictionary Headers
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string ReasonPhrase
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int StatusCode
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
     
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {

        }
    }
}

