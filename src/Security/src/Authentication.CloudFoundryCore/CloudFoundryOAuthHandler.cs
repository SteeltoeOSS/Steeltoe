// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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

            return new Dictionary<string, string>
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
            user ??= string.Empty;
            password ??= string.Empty;

            return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
        }

        protected internal virtual HttpClient GetHttpClient()
        {
            return Backchannel;
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            _logger?.LogDebug("ExchangeCodeAsync({code}, {redirectUri})", context.Code, context.RedirectUri);

            var options = Options.BaseOptions();
            options.CallbackUrl = context.RedirectUri;

            var tEx = new TokenExchanger(options, GetHttpClient());
            var response = await tEx.ExchangeCodeForToken(context.Code, Options.TokenEndpoint, Context.RequestAborted).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                _logger?.LogDebug("ExchangeCodeAsync() received json: {json}", result);
                var payload = JsonDocument.Parse(result);
                var tokenResponse = OAuthTokenResponse.Success(payload);

                return tokenResponse;
            }
            else
            {
                var error = $"OAuth token endpoint failure: {await Display(response).ConfigureAwait(false)}";
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        }

        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {
            _logger?.LogDebug("CreateTicketAsync()");

            var request = GetTokenInfoRequestMessage(tokens);
            var client = GetHttpClient();

            HttpClientHelper.ConfigureCertificateValidation(
                Options.ValidateCertificates,
                out var prevProtocols,
                out var prevValidator);

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request, Context.RequestAborted).ConfigureAwait(false);
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

            var resp = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            _logger?.LogDebug("CreateTicketAsync() received json: {json}", resp);
            var payload = JsonDocument.Parse(resp).RootElement;
            var context = new OAuthCreatingTicketContext(new ClaimsPrincipal(identity), properties, Context, Scheme, Options, Backchannel, tokens, payload);
            context.RunClaimActions();
            await Events.CreatingTicket(context).ConfigureAwait(false);

            if (Options.UseTokenLifetime)
            {
                properties.IssuedUtc = CloudFoundryHelper.GetIssueTime(payload);
                properties.ExpiresUtc = CloudFoundryHelper.GetExpTime(payload);
            }

            await Events.CreatingTicket(context).ConfigureAwait(false);
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
            if (!properties.Items.TryGetValue(name, out var value))
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
            output.Append($"Status: {response.StatusCode};");
            output.Append($"Headers: {response.Headers};");
            output.Append($"Body: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)};");
            return output.ToString();
        }
    }
}