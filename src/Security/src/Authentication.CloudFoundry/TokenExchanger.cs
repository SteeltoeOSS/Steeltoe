// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Steeltoe.Common.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class TokenExchanger
{
    private readonly ILogger _logger;
    private readonly AuthServerOptions _options;
    private readonly HttpClient _httpClient;

    public TokenExchanger(AuthServerOptions options, HttpClient httpClient = null, ILogger logger = null)
    {
        _options = options;
        _httpClient = httpClient ?? HttpClientHelper.GetHttpClient(options.ValidateCertificates, options.ClientTimeout);
        _logger = logger;
    }

    /// <summary>
    /// Perform the HTTP call to exchange an authorization code for a token.
    /// </summary>
    /// <param name="code">
    /// The auth code to exchange.
    /// </param>
    /// <param name="targetUrl">
    /// The full address of the token endpoint.
    /// </param>
    /// <param name="cancellationToken">
    /// Your CancellationToken.
    /// </param>
    /// <returns>
    /// The response from the remote server.
    /// </returns>
    public async Task<HttpResponseMessage> ExchangeCodeForTokenAsync(string code, string targetUrl, CancellationToken cancellationToken)
    {
        List<KeyValuePair<string, string>> requestParameters = AuthCodeTokenRequestParameters(code);
        HttpRequestMessage requestMessage = GetTokenRequestMessage(requestParameters, targetUrl);
        _logger?.LogDebug("Exchanging code {code} for token at {accessTokenUrl}", code, targetUrl);

        return await _httpClient.SendAsync(requestMessage, cancellationToken);
    }

    /// <summary>
    /// Passes an authorization code to OAuth server, maps server's <see cref="OpenIdTokenResponse" /> mapped to <see cref="ClaimsIdentity" />.
    /// </summary>
    /// <param name="code">
    /// Auth code received after user logs in at remote server.
    /// </param>
    /// <returns>
    /// The user's ClaimsIdentity.
    /// </returns>
    public async Task<ClaimsIdentity> ExchangeAuthCodeForClaimsIdentityAsync(string code)
    {
        HttpResponseMessage response = await ExchangeCodeForTokenAsync(code, _options.AuthorizationUrl, default);

        if (response.IsSuccessStatusCode)
        {
            _logger?.LogTrace("Successfully exchanged auth code for a token");
            var tokens = JsonSerializer.Deserialize<OpenIdTokenResponse>(await response.Content.ReadAsStringAsync());
#if DEBUG
            _logger?.LogTrace("Identity token received: {identityToken}", tokens.IdentityToken);
            _logger?.LogTrace("Access token received: {accessToken}", tokens.AccessToken);
#endif
            var securityToken = new JwtSecurityToken(tokens.IdentityToken);

            return BuildIdentityWithClaims(securityToken.Claims, tokens.Scope, tokens.AccessToken);
        }

        _logger?.LogError("Failed call to exchange code for token: {status}", response.StatusCode);
        _logger?.LogWarning(response.ReasonPhrase);
        _logger?.LogInformation(await response.Content.ReadAsStringAsync());

        return null;
    }

    /// <summary>
    /// Get an access token using client_credentials grant.
    /// </summary>
    /// <param name="targetUrl">
    /// full address of the token endpoint at the auth server.
    /// </param>
    /// <returns>
    /// HttpResponse from the auth server.
    /// </returns>
    public async Task<HttpResponseMessage> GetAccessTokenWithClientCredentialsAsync(string targetUrl)
    {
        HttpRequestMessage requestMessage = GetTokenRequestMessage(ClientCredentialsTokenRequestParameters(), targetUrl);

        return await _httpClient.SendAsync(requestMessage);
    }

    /// <summary>
    /// Builds an <see cref="HttpRequestMessage" /> that will POST with the params to the target.
    /// </summary>
    /// <param name="parameters">
    /// Body of the request to send.
    /// </param>
    /// <param name="targetUrl">
    /// Location to send the request.
    /// </param>
    /// <returns>
    /// A request primed for receiving a token.
    /// </returns>
    internal HttpRequestMessage GetTokenRequestMessage(List<KeyValuePair<string, string>> parameters, string targetUrl)
    {
        var requestContent = new FormUrlEncodedContent(parameters);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, targetUrl);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Content = requestContent;
        return requestMessage;
    }

    /// <summary>
    /// Gets request parameters for authorization_code Token request.
    /// </summary>
    /// <param name="code">
    /// Authorization code to be exchanged for token.
    /// </param>
    /// <returns>
    /// Content for HTTP request.
    /// </returns>
    internal List<KeyValuePair<string, string>> AuthCodeTokenRequestParameters(string code)
    {
        List<KeyValuePair<string, string>> parameters = CommonTokenRequestParams();
        parameters.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsRedirectUri, _options.CallbackUrl));
        parameters.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsCode, code));
        parameters.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsGrantType, OpenIdConnectGrantTypes.AuthorizationCode));

        return parameters;
    }

    internal List<KeyValuePair<string, string>> ClientCredentialsTokenRequestParameters()
    {
        List<KeyValuePair<string, string>> parameters = CommonTokenRequestParams();
        parameters.Add(new KeyValuePair<string, string>(CloudFoundryDefaults.ParamsGrantType, OpenIdConnectGrantTypes.ClientCredentials));

        return parameters;
    }

    internal List<KeyValuePair<string, string>> CommonTokenRequestParams()
    {
        string scopes = $"openid {_options.AdditionalTokenScopes}";

        if (_options.RequiredScopes != null)
        {
            scopes = $"{scopes.Trim()} {string.Join(" ", _options.RequiredScopes)}";
        }

        return new List<KeyValuePair<string, string>>
        {
            new(CloudFoundryDefaults.ParamsClientId, _options.ClientId),
            new(CloudFoundryDefaults.ParamsClientSecret, _options.ClientSecret),
            new(CloudFoundryDefaults.ParamsScope, scopes)
        };
    }

    internal ClaimsIdentity BuildIdentityWithClaims(IEnumerable<Claim> claims, string tokenScopes, string accessToken)
    {
        _logger?.LogTrace("Building identity with claims from token");
#if DEBUG
        foreach (Claim claim in claims)
        {
            _logger?.LogTrace("{type} : {value}", claim.Type, claim.Value);
        }
#endif
        string[] typedClaimNames =
        {
            "user_name",
            "email",
            "user_id"
        };

        IEnumerable<Claim> typedClaims = claims.Where(t => !typedClaimNames.Contains(t.Type, StringComparer.OrdinalIgnoreCase));

        // raw dump of claims, exclude mapped typedClaimNames
        var claimsId = new ClaimsIdentity(typedClaims, _options.SignInAsAuthenticationType);

        string userName = claims.First(c => c.Type == "user_name").Value;
        string email = claims.First(c => c.Type == "email").Value;
        string userId = claims.First(c => c.Type == "user_id").Value;

        claimsId.AddClaims(new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.Email, email)
        });

        _logger?.LogTrace("Adding scope claims from token");
        IEnumerable<string> additionalScopes = tokenScopes.Split(' ').Where(s => s != "openid");

        foreach (string scope in additionalScopes)
        {
            claimsId.AddClaim(new Claim("scope", scope));
        }

        claimsId.AddClaim(new Claim(ClaimTypes.Authentication, accessToken));
        _logger?.LogTrace("Finished building identity with claims from token");

        return claimsId;
    }
}
