// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry;

public class CloudFoundryTokenKeyResolver
{
#if NET8_0_OR_GREATER
    private static readonly Random _randomShared = Random.Shared;
#else
    private static readonly Random _randomShared = new ();
#endif

    private static readonly TimeSpan _cacheTimeToLiveForKeyFound = TimeSpan.FromHours(12);
    private static readonly TimeSpan _cacheMinTimeToLiveForKeyNotFound = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _cacheMaxTimeToLiveForKeyNotFound = TimeSpan.FromSeconds(60);

    private readonly string _jwtKeyUrl;
    private readonly HttpMessageHandler _httpHandler;
    private readonly bool _validateCertificates;
    private readonly int _httpClientTimeoutMillis;
    private HttpClient _httpClient;

    internal static MemoryCache Cache { get; set; } = new (new MemoryCacheOptions());

    public CloudFoundryTokenKeyResolver(string jwtKeyUrl, HttpMessageHandler httpHandler, bool validateCertificates)
    {
        if (string.IsNullOrEmpty(jwtKeyUrl))
        {
            throw new ArgumentException("Value must not be null or empty.", nameof(jwtKeyUrl));
        }

        _jwtKeyUrl = jwtKeyUrl;
        _httpHandler = httpHandler;
        _validateCertificates = validateCertificates;
        _httpClientTimeoutMillis = 100000;
    }

    public CloudFoundryTokenKeyResolver(string jwtKeyUrl, HttpMessageHandler httpHandler, bool validateCertificates, int httpClientTimeoutMS)
    {
        if (string.IsNullOrEmpty(jwtKeyUrl))
        {
            throw new ArgumentException("Value must not be null or empty.", nameof(jwtKeyUrl));
        }

        _jwtKeyUrl = jwtKeyUrl;
        _httpHandler = httpHandler;
        _validateCertificates = validateCertificates;
        _httpClientTimeoutMillis = httpClientTimeoutMS;
    }

    public virtual IEnumerable<SecurityKey> ResolveSigningKey(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
    {
        var cacheKey = GetCacheKey(kid);

        if (!Cache.TryGetValue(cacheKey, out SecurityKey matchingWebKey))
        {
            var webKeySet = FetchKeySet().GetAwaiter().GetResult();

            foreach (var nextWebKey in webKeySet?.Keys ?? Array.Empty<JsonWebKey>())
            {
                FixupKey(nextWebKey);
                var nextCacheKey = GetCacheKey(nextWebKey.Kid);
                Cache.Set(nextCacheKey, nextWebKey, _cacheTimeToLiveForKeyFound);

                if (nextWebKey.Kid == kid)
                {
                    matchingWebKey = nextWebKey;
                }
            }

            if (matchingWebKey == null)
            {
                var timeToLive = GetTimeToLiveForNotFound();
                Cache.Set<JsonWebKey>(cacheKey, null, timeToLive);
            }
        }

        return matchingWebKey == null ? new List<SecurityKey>() : new List<SecurityKey> { matchingWebKey };
    }

    public JsonWebKey FixupKey(JsonWebKey key)
    {
        if (Platform.IsFullFramework)
        {
            var existing = Base64UrlEncoder.DecodeBytes(key.N);
            TrimKey(key, existing);
        }

        return key;
    }

    public virtual async Task<JsonWebKeySet> FetchKeySet()
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, _jwtKeyUrl);
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var client = GetHttpClient();

        HttpClientHelper.ConfigureCertificateValidation(
            _validateCertificates,
            out var prevProtocols,
            out var prevValidator);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(requestMessage).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
        finally
        {
            HttpClientHelper.RestoreCertificateValidation(_validateCertificates, prevProtocols, prevValidator);
        }

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return GetJsonWebKeySet(result);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        return null;
    }

    public virtual JsonWebKeySet GetJsonWebKeySet(string json)
    {
        return JsonWebKeySet.Create(json);
    }

    public virtual HttpClient GetHttpClient()
    {
        if (_httpClient == null)
        {
            if (_httpHandler is null)
            {
                _httpClient = HttpClientHelper.GetHttpClient(_validateCertificates, _httpClientTimeoutMillis);
            }
            else
            {
                _httpClient = HttpClientHelper.GetHttpClient(_httpHandler);
            }
        }

        return _httpClient;
    }

    private string GetCacheKey(string keyId)
    {
        return $"{_jwtKeyUrl}:{keyId}";
    }

    private TimeSpan GetTimeToLiveForNotFound()
    {
        var jitterSeconds = _randomShared.NextDouble() * (_cacheMaxTimeToLiveForKeyNotFound - _cacheMinTimeToLiveForKeyNotFound).TotalSeconds;
        return _cacheMinTimeToLiveForKeyNotFound + TimeSpan.FromSeconds(jitterSeconds);
    }

    private void TrimKey(JsonWebKey key, byte[] existing)
    {
        var signRemoved = new byte[existing.Length - 1];
        Buffer.BlockCopy(existing, 1, signRemoved, 0, existing.Length - 1);
        var withSignRemoved = Base64UrlEncoder.Encode(signRemoved);
        key.N = withSignRemoved;
    }
}