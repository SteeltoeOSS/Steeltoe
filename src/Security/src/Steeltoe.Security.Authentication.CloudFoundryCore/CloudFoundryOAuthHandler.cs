// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryOAuthHandler : OAuthHandler<CloudFoundryOAuthOptions>
    {
        private readonly ILogger<CloudFoundryOAuthHandler> _logger;

        public CloudFoundryOAuthHandler(
             IOptionsMonitor<CloudFoundryOAuthOptions> options,
             ILoggerFactory logger,
             UrlEncoder encoder,
             ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            _logger = logger?.CreateLogger<CloudFoundryOAuthHandler>();
        }

        protected internal virtual Dictionary<string, string> GetTokenInfoRequestParameters(OAuthTokenResponse tokens)
        {
            _logger?.LogDebug("GetTokenInfoRequestParameters() using token: {token}", tokens.AccessToken);

            return new Dictionary<string, string>()
            {
                { "token", tokens.AccessToken }
            };
        }

        protected internal virtual HttpRequestMessage GetTokenInfoRequestMessage(OAuthTokenResponse tokens)
        {
            _logger?.LogDebug("GetTokenInfoRequestMessage({token}) with {clientId}", tokens.AccessToken, Options.ClientId);

            var tokenRequestParameters = GetTokenInfoRequestParameters(tokens);

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);
            var request = new HttpRequestMessage(HttpMethod.Post, Options.TokenInfoUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", GetEncoded(Options.ClientId, Options.ClientSecret));
            request.Content = requestContent;
            return request;
        }

        protected internal string GetEncoded(string user, string password)
        {
            if (user == null)
            {
                user = string.Empty;
            }

            if (password == null)
            {
                password = string.Empty;
            }

            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));
        }

        protected internal virtual HttpClient GetHttpClient()
        {
            return Backchannel;
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {
            _logger?.LogDebug("ExchangeCodeAsync({code}, {redirectUri})", code, redirectUri);

            var options = Options.BaseOptions();
            options.CallbackUrl = redirectUri;

            var tEx = new TokenExchanger(options, GetHttpClient());
            HttpResponseMessage response = await tEx.ExchangeCodeForToken(code, Options.TokenEndpoint, Context.RequestAborted);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                _logger?.LogDebug("ExchangeCodeAsync() received json: {json}", result);

                var payload = JObject.Parse(result);
                var tokenResponse = OAuthTokenResponse.Success(payload);
                return tokenResponse;
            }
            else
            {
                var error = "OAuth token endpoint failure: " + await Display(response);
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            _logger?.LogDebug("CreateTicketAsync()");

            HttpRequestMessage request = GetTokenInfoRequestMessage(tokens);
            HttpClient client = GetHttpClient();

            HttpClientHelper.ConfigureCertificateValidation(
                Options.ValidateCertificates,
                out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request, Context.RequestAborted);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(Options.ValidateCertificates, prevProtocols, prevValidator);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogDebug("CreateTicketAsync() failure getting token info from {requesturi}", request.RequestUri);
                throw new HttpRequestException($"An error occurred when retrieving token information ({response.StatusCode}).");
            }

            var resp = await response.Content.ReadAsStringAsync();

            _logger?.LogDebug("CreateTicketAsync() received json: {json}", resp);

            var payload = JObject.Parse(resp);

            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload);
            context.RunClaimActions();
            await Events.CreatingTicket(context);

            if (Options.UseTokenLifetime)
            {
                properties.IssuedUtc = CloudFoundryHelper.GetIssueTime(payload);
                properties.ExpiresUtc = CloudFoundryHelper.GetExpTime(payload);
            }

            await Events.CreatingTicket(context);
            var ticket = new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
            return ticket;
        }

        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            _logger?.LogDebug("BuildChallengeUrl({redirectUri}) with {clientId}", redirectUri, Options.ClientId);

            var scope = FormatScope();

            var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { CloudFoundryDefaults.ParamsResponseType, "code" },
                { CloudFoundryDefaults.ParamsClientId, Options.ClientId },
                { CloudFoundryDefaults.ParamsRedirectUri, redirectUri }
            };

            AddQueryString(queryStrings, properties, "scope", scope);

            if (Options.StateDataFormat != null)
            {
                var state = Options.StateDataFormat.Protect(properties);
                queryStrings.Add("state", state);
            }

            var authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
            return authorizationEndpoint;
        }

        private static void AddQueryString(IDictionary<string, string> queryStrings, AuthenticationProperties properties, string name, string defaultValue = null)
        {
            if (!properties.Items.TryGetValue(name, out string value))
            {
                value = defaultValue;
            }
            else
            {
                properties.Items.Remove(name);
            }

            if (value == null)
            {
                return;
            }

            queryStrings[name] = value;
        }

        private static async Task<string> Display(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers.ToString() + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync() + ";");
            return output.ToString();
        }
    }
}