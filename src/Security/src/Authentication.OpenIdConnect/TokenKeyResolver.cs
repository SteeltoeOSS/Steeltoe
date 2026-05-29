// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common.Extensions;

namespace Steeltoe.Security.Authentication.OpenIdConnect;

internal sealed partial class TokenKeyResolver : IDisposable
{
    private static readonly MediaTypeWithQualityHeaderValue AcceptHeader = new("application/json");
    private static readonly TimeSpan CacheTimeToLiveForKeyFound = TimeSpan.FromHours(12);
    private static readonly TimeSpan CacheMinTimeToLiveForKeyNotFound = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CacheMaxTimeToLiveForKeyNotFound = TimeSpan.FromSeconds(60);
    private readonly MemoryCache _cache;
    private readonly ILogger<TokenKeyResolver> _logger;

    public TokenKeyResolver(TimeProvider timeProvider, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _cache = new MemoryCache(new MemoryCacheOptions
        {
            Clock = new TimeProviderSystemClock(timeProvider)
        }, loggerFactory);

        _logger = loggerFactory.CreateLogger<TokenKeyResolver>();
    }

    internal JsonWebKey? ResolveSigningKey(string authority, string keyId, HttpClient httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentNullException.ThrowIfNull(httpClient);

        Uri tokenKeysUri = GetTokenKeysUri(authority);
        return CachingResolveSigningKey(tokenKeysUri, keyId, httpClient);
    }

    private static Uri GetTokenKeysUri(string authority)
    {
        if (!authority.EndsWith('/'))
        {
            authority += '/';
        }

        var authorityUri = new Uri(authority);
        return new Uri(authorityUri, "token_keys");
    }

    private JsonWebKey? CachingResolveSigningKey(Uri tokenKeysUri, string keyId, HttpClient httpClient)
    {
        string cacheKey = GetCacheKey(tokenKeysUri, keyId);

        if (!_cache.TryGetValue<JsonWebKey?>(cacheKey, out JsonWebKey? matchingWebKey))
        {
            JsonWebKeySet? webKeySet = FetchKeySet(tokenKeysUri, httpClient);

            foreach (JsonWebKey nextWebKey in webKeySet?.Keys ?? [])
            {
                string nextCacheKey = GetCacheKey(tokenKeysUri, nextWebKey.Kid);
                _cache.Set(nextCacheKey, nextWebKey, CacheTimeToLiveForKeyFound);

                if (nextWebKey.Kid == keyId)
                {
                    matchingWebKey = nextWebKey;
                }
            }

            if (matchingWebKey == null)
            {
                TimeSpan timeToLive = GetTimeToLiveForNotFound();
                _cache.Set<JsonWebKey?>(cacheKey, null, timeToLive);

                if (webKeySet == null)
                {
                    LogDisableFetchAfterServerError(keyId, (int)timeToLive.TotalSeconds);
                }
                else
                {
                    LogDisableFetchAfterKeyNotFound(keyId, (int)timeToLive.TotalSeconds);
                }
            }
        }

        return matchingWebKey;
    }

    private static string GetCacheKey(Uri tokenKeysUri, string keyId)
    {
        return $"{tokenKeysUri}:{keyId}";
    }

    private static TimeSpan GetTimeToLiveForNotFound()
    {
        double jitterSeconds = Random.Shared.NextDouble() * (CacheMaxTimeToLiveForKeyNotFound - CacheMinTimeToLiveForKeyNotFound).TotalSeconds;
        return CacheMinTimeToLiveForKeyNotFound + TimeSpan.FromSeconds(jitterSeconds);
    }

    private JsonWebKeySet? FetchKeySet(Uri tokenKeysUri, HttpClient httpClient)
    {
#pragma warning disable S4462 // Calls to "async" methods should not be blocking
        // Justification: can't be async all the way until updates are complete in Microsoft libraries
        // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/468
        return FetchKeySetAsync(tokenKeysUri, httpClient, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore S4462 // Calls to "async" methods should not be blocking
    }

    private async Task<JsonWebKeySet?> FetchKeySetAsync(Uri tokenKeysUri, HttpClient httpClient, CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, tokenKeysUri);
        requestMessage.Headers.Accept.Add(AcceptHeader);

        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException || exception.IsHttpClientTimeout())
        {
            LogTokenKeysEndpointUnreachable(exception, tokenKeysUri);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            LogFetchTokenKeysStatusFailed(tokenKeysUri, (int)response.StatusCode);
            return null;
        }

        try
        {
            string result = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonWebKeySet.Create(result);
        }
        catch (ArgumentException exception)
        {
            LogFetchTokenKeysParseFailed(exception, tokenKeysUri);
            return null;
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [LoggerMessage(LogLevel.Warning, "Fetch keys from '{TokenKeysUri}' failed.")]
    private partial void LogTokenKeysEndpointUnreachable(Exception exception, MaskedUri tokenKeysUri);

    [LoggerMessage(LogLevel.Warning, "Fetch keys from '{TokenKeysUri}' failed with HTTP status {StatusCode}.")]
    private partial void LogFetchTokenKeysStatusFailed(MaskedUri tokenKeysUri, int statusCode);

    [LoggerMessage(LogLevel.Warning, "Fetch keys from '{TokenKeysUri}' failed because the returned JSON is invalid.")]
    private partial void LogFetchTokenKeysParseFailed(Exception exception, MaskedUri tokenKeysUri);

    [LoggerMessage(LogLevel.Information, "Disabled fetch for key '{KeyId}' for {RetryAfterSeconds}s because the HTTP request failed.")]
    private partial void LogDisableFetchAfterServerError(string keyId, int retryAfterSeconds);

    [LoggerMessage(LogLevel.Information, "Disabled fetch for key '{KeyId}' for {RetryAfterSeconds}s because the key was not found in the HTTP response.")]
    private partial void LogDisableFetchAfterKeyNotFound(string keyId, int retryAfterSeconds);

    private sealed class TimeProviderSystemClock : ISystemClock
    {
        private readonly TimeProvider _timeProvider;

        public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

        public TimeProviderSystemClock(TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            _timeProvider = timeProvider;
        }
    }
}
