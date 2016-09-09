//
// Copyright 2015 the original author or authors.
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


using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http.Authentication;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace SteelToe.Security.Authentication.CloudFoundry
{
    internal class CloudFoundryHandler : OAuthHandler<CloudFoundryOptions>
    {
        public CloudFoundryHandler(HttpClient httpClient)
            : base(httpClient)
        {
        }

        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(string code, string redirectUri)
        {

            HttpRequestMessage requestMessage = GetTokenRequestMessage(code, redirectUri);
            HttpClient client = GetHttpClient();
#if NET451
            RemoteCertificateValidationCallback prevValidator = null;
            if (!Options.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(requestMessage, Context.RequestAborted);
            }
            finally
            {
#if NET451
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
#endif
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var payload = JObject.Parse(result);
                return OAuthTokenResponse.Success(payload);
            }
            else
            {
                var error = "OAuth token endpoint failure: " + await Display(response);
                return OAuthTokenResponse.Failed(new Exception(error));
            }
        }


        protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
        {

            HttpRequestMessage request = GetTokenInfoRequestMessage(tokens);
            HttpClient client = GetHttpClient();

#if NET451
            RemoteCertificateValidationCallback prevValidator = null;
            if (!Options.ValidateCertificates)
            {
            prevValidator = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(request, Context.RequestAborted);
            }
            finally
            {
#if NET451
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
#endif
            }

            response.EnsureSuccessStatusCode();

            var resp = await response.Content.ReadAsStringAsync();
            var payload = JObject.Parse(resp);

            var identifier = CloudFoundryHelper.GetId(payload);
            if (!string.IsNullOrEmpty(identifier))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var givenName = CloudFoundryHelper.GetGivenName(payload);
            if (!string.IsNullOrEmpty(givenName))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, givenName, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var familyName = CloudFoundryHelper.GetFamilyName(payload);
            if (!string.IsNullOrEmpty(familyName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Surname, familyName, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var name = CloudFoundryHelper.GetName(payload);
            if (!string.IsNullOrEmpty(name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, name, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var email = CloudFoundryHelper.GetEmail(payload);
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, email, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            var scopes = CloudFoundryHelper.GetScopes(payload);
            if (scopes != null)
            {
                foreach (var s in scopes)
                {
                    identity.AddClaim(new Claim(s, string.Empty, ClaimValueTypes.String, Options.ClaimsIssuer));
                }
            }

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Options.AuthenticationScheme);
            var context = new OAuthCreatingTicketContext(ticket, Context, Options, Backchannel, tokens, payload);

            await Options.Events.CreatingTicket(context);

            return context.Ticket;
        }


        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            var scope = FormatScope();

            var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            queryStrings.Add("response_type", "code");
            queryStrings.Add("client_id", Options.ClientId);
            queryStrings.Add("redirect_uri", redirectUri);

            AddQueryString(queryStrings, properties, "scope", scope);

            if (Options.StateDataFormat != null)
            {
                var state = Options.StateDataFormat.Protect(properties);
                queryStrings.Add("state", state);
            }

            var authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
            return authorizationEndpoint;
        }

        internal protected virtual Dictionary<string, string> GetTokenInfoRequestParameters(OAuthTokenResponse tokens)
        {
            return new Dictionary<string, string>()
            {
                { "token", tokens.AccessToken }

            };
        }

        internal protected virtual HttpRequestMessage GetTokenInfoRequestMessage(OAuthTokenResponse tokens)
        {
            var tokenRequestParameters = GetTokenInfoRequestParameters(tokens);

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);
            var request = new HttpRequestMessage(HttpMethod.Post, Options.TokenInfoUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", GetEncoded(Options.ClientId, Options.ClientSecret));
            request.Content = requestContent;
            return request;
        }

        internal protected virtual Dictionary<string, string> GetTokenRequestParameters(string code, string redirectUri)
        {
            return new Dictionary<string, string>()
            {
                { "client_id", Options.ClientId },
                { "redirect_uri", redirectUri },
                { "client_secret", Options.ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
            };
        }

        internal protected virtual HttpRequestMessage GetTokenRequestMessage(string code, string redirectUri)
        {
            var tokenRequestParameters = GetTokenRequestParameters(code, redirectUri);

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            return requestMessage;
        }

        internal protected string GetEncoded(string user, string password)
        {
            if (user == null)
                user = string.Empty;
            if (password == null)
                password = string.Empty;
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));
        }

        internal protected virtual HttpClient GetHttpClient()
        {
            return Backchannel;
        }

        private static void AddQueryString(IDictionary<string, string> queryStrings, AuthenticationProperties properties, string name, string defaultValue = null)
        {
            string value;
            if (!properties.Items.TryGetValue(name, out value))
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