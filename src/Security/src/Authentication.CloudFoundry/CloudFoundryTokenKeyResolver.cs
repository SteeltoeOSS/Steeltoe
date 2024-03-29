// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common;
using Steeltoe.Common.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryTokenKeyResolver
{
    private readonly string _jwtKeyUrl;
    private readonly HttpMessageHandler _httpHandler;
    private readonly bool _validateCertificates;
    private readonly int _httpClientTimeoutMillis;
    private HttpClient _httpClient;

    internal static ConcurrentDictionary<string, SecurityKey> Resolved { get; set; } = new();

    public CloudFoundryTokenKeyResolver(string jwtKeyUrl, HttpMessageHandler httpHandler, bool validateCertificates)
    {
        ArgumentGuard.NotNullOrEmpty(jwtKeyUrl);

        _jwtKeyUrl = jwtKeyUrl;
        _httpHandler = httpHandler;
        _validateCertificates = validateCertificates;
        _httpClientTimeoutMillis = 100000;
    }

    public CloudFoundryTokenKeyResolver(string jwtKeyUrl, HttpMessageHandler httpHandler, bool validateCertificates, int httpClientTimeoutMs)
    {
        ArgumentGuard.NotNullOrEmpty(jwtKeyUrl);

        _jwtKeyUrl = jwtKeyUrl;
        _httpHandler = httpHandler;
        _validateCertificates = validateCertificates;
        _httpClientTimeoutMillis = httpClientTimeoutMs;
    }

    public virtual IEnumerable<SecurityKey> ResolveSigningKey(string token, SecurityToken securityToken, string kid,
        TokenValidationParameters validationParameters)
    {
        if (Resolved.TryGetValue(kid, out SecurityKey resolved))
        {
            return new List<SecurityKey>
            {
                resolved
            };
        }

        JsonWebKeySet keySet = FetchKeySetAsync().GetAwaiter().GetResult();

        if (keySet != null)
        {
            foreach (JsonWebKey key in keySet.Keys)
            {
                Resolved[key.Kid] = key;
            }
        }

        if (Resolved.TryGetValue(kid, out resolved))
        {
            return new List<SecurityKey>
            {
                resolved
            };
        }

        return null;
    }

    public virtual async Task<JsonWebKeySet> FetchKeySetAsync()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_jwtKeyUrl));
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpClient client = GetHttpClient();

        HttpResponseMessage response = await client.SendAsync(requestMessage);

        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync();
            return GetJsonWebKeySet(result);
        }

        return null;
    }

    public virtual JsonWebKeySet GetJsonWebKeySet(string json)
    {
        return JsonWebKeySet.Create(json);
    }

    public virtual HttpClient GetHttpClient()
    {
        _httpClient ??= _httpHandler is null
            ? HttpClientHelper.GetHttpClient(_validateCertificates, _httpClientTimeoutMillis)
            : HttpClientHelper.GetHttpClient(_httpHandler);

        return _httpClient;
    }
}
