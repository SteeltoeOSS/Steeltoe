// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryOAuthHandler : OAuthHandler<CloudFoundryOAuthOptions>
{
    private readonly ILogger<CloudFoundryOAuthHandler> _logger;

    public CloudFoundryOAuthHandler(IOptionsMonitor<CloudFoundryOAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
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

        Dictionary<string, string> tokenRequestParameters = GetTokenInfoRequestParameters(tokens);

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

        AuthServerOptions options = Options.BaseOptions();
        options.CallbackUrl = context.RedirectUri;

        var tEx = new TokenExchanger(options, GetHttpClient());
        HttpResponseMessage response = await tEx.ExchangeCodeForTokenAsync(context.Code, Options.TokenEndpoint, Context.RequestAborted).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            _logger?.LogDebug("ExchangeCodeAsync() received json: {json}", result);
            JsonDocument payload = JsonDocument.Parse(result);
            OAuthTokenResponse tokenResponse = OAuthTokenResponse.Success(payload);

            return tokenResponse;
        }

        string error = $"OAuth token endpoint failure: {await DisplayAsync(response).ConfigureAwait(false)}";
        return OAuthTokenResponse.Failed(new Exception(error));
    }

    protected override async Task<AuthenticationTicket> CreateTicketAsync(ClaimsIdentity identity, AuthenticationProperties properties,
        OAuthTokenResponse tokens)
    {
        _logger?.LogDebug("CreateTicketAsync()");

        HttpRequestMessage request = GetTokenInfoRequestMessage(tokens);
        HttpClient client = GetHttpClient();

        HttpClientHelper.ConfigureCertificateValidation(Options.ValidateCertificates, out SecurityProtocolType prevProtocols,
            out RemoteCertificateValidationCallback prevValidator);

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
            _logger?.LogDebug("CreateTicketAsync() failure getting token info from {requestUri}", request.RequestUri);
            throw new HttpRequestException($"An error occurred while retrieving token information ({response.StatusCode}).");
        }

        string resp = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        _logger?.LogDebug("CreateTicketAsync() received json: {json}", resp);
        JsonElement payload = JsonDocument.Parse(resp).RootElement;
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

        string scope = FormatScope();

        var queryStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { CloudFoundryDefaults.ParamsResponseType, "code" },
            { CloudFoundryDefaults.ParamsClientId, Options.ClientId },
            { CloudFoundryDefaults.ParamsRedirectUri, redirectUri }
        };

        AddQueryString(queryStrings, properties, "scope", scope);

        if (Options.StateDataFormat != null)
        {
            string state = Options.StateDataFormat.Protect(properties);
            queryStrings.Add("state", state);
        }

        string authorizationEndpoint = QueryHelpers.AddQueryString(Options.AuthorizationEndpoint, queryStrings);
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

    private static async Task<string> DisplayAsync(HttpResponseMessage response)
    {
        var output = new StringBuilder();
        output.Append($"Status: {response.StatusCode};");
        output.Append($"Headers: {response.Headers};");
        output.Append($"Body: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)};");
        return output.ToString();
    }
}
