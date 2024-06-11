// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common;

namespace Steeltoe.Security.Authentication.Shared;

internal sealed class TokenKeyResolver
{
    private readonly HttpClient _httpClient;
    private string _authority;

    internal static ConcurrentDictionary<string, SecurityKey> Resolved { get; } = new();

    public TokenKeyResolver(string authority, HttpClient httpClient)
    {
        ArgumentGuard.NotNull(authority);
        ArgumentGuard.NotNull(httpClient);

        _authority = authority;
        _httpClient = httpClient;
    }

    internal IEnumerable<SecurityKey> ResolveSigningKey(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
    {
        if (Resolved.TryGetValue(kid, out SecurityKey? resolved))
        {
            return new List<SecurityKey>
            {
                resolved
            };
        }

        JsonWebKeySet? keySet = FetchKeySetAsync().GetAwaiter().GetResult();

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

        return [];
    }

    internal async Task<JsonWebKeySet?> FetchKeySetAsync()
    {
        if (!_authority.EndsWith("/", StringComparison.Ordinal))
        {
            _authority += "/";
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"{_authority}token_keys"));
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string result = await response.Content.ReadAsStringAsync();
        return JsonWebKeySet.Create(result);
    }
}
