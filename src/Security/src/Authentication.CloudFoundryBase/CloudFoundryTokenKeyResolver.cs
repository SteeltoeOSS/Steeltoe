// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
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
        if (string.IsNullOrEmpty(jwtKeyUrl))
        {
            throw new ArgumentException(nameof(jwtKeyUrl));
        }

        _jwtKeyUrl = jwtKeyUrl;
        _httpHandler = httpHandler;
        _validateCertificates = validateCertificates;
        _httpClientTimeoutMillis = 100000;
    }

    public CloudFoundryTokenKeyResolver(string jwtKeyUrl, HttpMessageHandler httpHandler, bool validateCertificates, int httpClientTimeoutMs)
    {
        if (string.IsNullOrEmpty(jwtKeyUrl))
        {
            throw new ArgumentException(nameof(jwtKeyUrl));
        }

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

        JsonWebKeySet keySet = FetchKeySet().GetAwaiter().GetResult();

        if (keySet != null)
        {
            foreach (JsonWebKey key in keySet.Keys)
            {
                FixupKey(key);
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

    public JsonWebKey FixupKey(JsonWebKey key)
    {
        if (Platform.IsFullFramework)
        {
            byte[] existing = Base64UrlEncoder.DecodeBytes(key.N);
            TrimKey(key, existing);
        }

        return key;
    }

    public virtual async Task<JsonWebKeySet> FetchKeySet()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, _jwtKeyUrl);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpClient client = GetHttpClient();

        HttpClientHelper.ConfigureCertificateValidation(_validateCertificates, out SecurityProtocolType prevProtocols,
            out RemoteCertificateValidationCallback prevValidator);

        HttpResponseMessage response = null;

        try
        {
            response = await client.SendAsync(requestMessage).ConfigureAwait(false);
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, prevProtocols, prevValidator);
        }

        if (response.IsSuccessStatusCode)
        {
            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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

    private void TrimKey(JsonWebKey key, byte[] existing)
    {
        byte[] signRemoved = new byte[existing.Length - 1];
        Buffer.BlockCopy(existing, 1, signRemoved, 0, existing.Length - 1);
        string withSignRemoved = Base64UrlEncoder.Encode(signRemoved);
        key.N = withSignRemoved;
    }
}
