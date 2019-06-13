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

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Steeltoe.Common.Http;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class TokenExchanger
    {
        private readonly ILogger _logger;
        private readonly AuthServerOptions _options;
        private readonly HttpClient _httpClient;

        public TokenExchanger(AuthServerOptions options, HttpClient httpclient = null, ILogger logger = null)
        {
            _options = options;
            _httpClient = httpclient ?? HttpClientHelper.GetHttpClient(options.ValidateCertificates, options.ClientTimeout);
            _logger = logger;
        }

        /// <summary>
        /// Perform the HTTP call to exchange an authorization code for a token
        /// </summary>
        /// <param name="code">The auth code to exchange</param>
        /// <param name="targetUrl">The full address of the token endpoint</param>
        /// <param name="cancellationToken">Your CancellationToken</param>
        /// <returns>The response from the remote server</returns>
        public async Task<HttpResponseMessage> ExchangeCodeForToken(string code, string targetUrl, CancellationToken cancellationToken)
        {
            var requestParameters = AuthCodeTokenRequestParameters(code);
            HttpRequestMessage requestMessage = GetTokenRequestMessage(requestParameters, targetUrl);
            _logger?.LogDebug("Exchanging code {code} for token at {accessTokenUrl}", code, targetUrl);

            HttpClientHelper.ConfigureCertificateValidation(
                _options.ValidateCertificates,
                out SecurityProtocolType protocolType,
                out RemoteCertificateValidationCallback prevValidator);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_options.ValidateCertificates, protocolType, prevValidator);
            }

            return response;
        }

        /// <summary>
        /// Passes an authorization code to OAuth server, maps server's <see cref="OpenIdTokenResponse"/> mapped to <see cref="ClaimsIdentity"/>
        /// </summary>
        /// <param name="code">Auth code received after user logs in at remote server</param>
        /// <returns>The user's ClaimsIdentity</returns>
        public async Task<ClaimsIdentity> ExchangeAuthCodeForClaimsIdentity(string code)
        {
            HttpResponseMessage response = await ExchangeCodeForToken(code, _options.AuthorizationUrl, default(CancellationToken));

            if (response.IsSuccessStatusCode)
            {
                _logger?.LogTrace("Successfully exchanged auth code for a token");
                var tokens = await response.Content.ReadAsJsonAsync<OpenIdTokenResponse>();
#if DEBUG
                _logger?.LogTrace("Identity token received: {identityToken}", tokens.IdentityToken);
                _logger?.LogTrace("Access token received: {accessToken}", tokens.AccessToken);
#endif
                JwtSecurityToken securityToken = new JwtSecurityToken(tokens.IdentityToken);

                return BuildIdentityWithClaims(securityToken.Claims, tokens.Scope, tokens.AccessToken);
            }
            else
            {
                _logger?.LogError("Failed call to exchange code for token : " + response.StatusCode);
                _logger?.LogWarning(response.ReasonPhrase);
                _logger?.LogInformation(await response.Content.ReadAsStringAsync());

                return null;
            }
        }

        /// <summary>
        /// Get an access token using client_credentials grant
        /// </summary>
        /// <param name="targetUrl">full address of the token endpoint at the auth server</param>
        /// <returns>HttpResponse from the auth server</returns>
        public async Task<HttpResponseMessage> GetAccessTokenWithClientCredentials(string targetUrl)
        {
            HttpRequestMessage requestMessage = GetTokenRequestMessage(ClientCredentialsTokenRequestParameters(), targetUrl);

            HttpClientHelper.ConfigureCertificateValidation(_options.ValidateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(requestMessage);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_options.ValidateCertificates, protocolType, prevValidator);
            }

            return response;
        }

        /// <summary>
        /// Builds an <see cref="HttpRequestMessage"/> that will POST with the params to the target
        /// </summary>
        /// <param name="parameters">Body of the request to send</param>
        /// <param name="targetUrl">Location to send the request</param>
        /// <returns>A request primed for receiving a token</returns>
        internal HttpRequestMessage GetTokenRequestMessage(List<KeyValuePair<string, string>> parameters, string targetUrl)
        {
            var requestContent = new FormUrlEncodedContent(parameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, targetUrl);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            return requestMessage;
        }

        /// <summary>
        /// Gets request parameters for authorization_code Token request
        /// </summary>
        /// <param name="code">Authorization code to be exchanged for token</param>
        /// <returns>Content for HTTP request</returns>
        internal List<KeyValuePair<string, string>> AuthCodeTokenRequestParameters(string code)
        {
            var parms = CommonTokenRequestParams();
            parms.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsRedirectUri, _options.CallbackUrl));
            parms.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsCode, code));
            parms.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsGrantType, OpenIdConnectGrantTypes.AuthorizationCode));

            return parms;
        }

        internal List<KeyValuePair<string, string>> ClientCredentialsTokenRequestParameters()
        {
            var parms = CommonTokenRequestParams();
            parms.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsGrantType, OpenIdConnectGrantTypes.ClientCredentials));

            return parms;
        }

        internal List<KeyValuePair<string, string>> CommonTokenRequestParams()
        {
            var scopes = "openid " + _options.AdditionalTokenScopes;
            if (_options.RequiredScopes != null)
            {
                scopes = string.Join(" ", scopes, _options.RequiredScopes);
            }

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsClientId, _options.ClientId),
                new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsClientSecret, _options.ClientSecret),
                new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsScope, scopes)
            };
        }

        internal ClaimsIdentity BuildIdentityWithClaims(IEnumerable<Claim> claims, string tokenScopes, string accessToken)
        {
            _logger?.LogTrace("Building identity with claims from token");
#if DEBUG
            foreach (var claim in claims)
            {
                _logger?.LogTrace(claim.Type + " : " + claim.Value);
            }
#endif
            var typedClaimNames = new[] { "user_name", "email", "user_id" };
            var typedClaims = claims.Where(t => !typedClaimNames.Contains(t.Type, System.StringComparer.OrdinalIgnoreCase));

            // raw dump of claims, exclude mapped typedClaimNames
            var claimsId = new ClaimsIdentity(typedClaims, _options.SignInAsAuthenticationType);

            string userName = claims.First(c => c.Type == "user_name").Value;
            string email = claims.First(c => c.Type == "email").Value;
            string userId = claims.First(c => c.Type == "user_id").Value;

            claimsId.AddClaims(new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name, userName),
                        new Claim(ClaimTypes.Email, email),
                    });

            _logger?.LogTrace("Adding scope claims from token");
            var additionalScopes = tokenScopes.Split(' ').Where(s => s != "openid");
            foreach (var scope in additionalScopes)
            {
                claimsId.AddClaim(new Claim("scope", scope));
            }

            claimsId.AddClaim(new Claim(ClaimTypes.Authentication, accessToken));
            _logger?.LogTrace("Finished building identity with claims from token");

            return claimsId;
        }
    }
}
