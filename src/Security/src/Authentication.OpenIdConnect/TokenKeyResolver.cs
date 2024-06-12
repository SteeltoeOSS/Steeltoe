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
    private readonly Uri _authority;
    private readonly MediaTypeWithQualityHeaderValue _acceptHeader = new("application/json");

    internal static ConcurrentDictionary<string, SecurityKey> ResolvedSecurityKeysById { get; } = new();

    public TokenKeyResolver(string authority, HttpClient httpClient)
    {
        ArgumentGuard.NotNull(authority);
        ArgumentGuard.NotNull(httpClient);

        if (!authority.EndsWith('/'))
        {
            authority += '/';
        }

        _authority = new Uri($"{authority}token_keys");
        _httpClient = httpClient;
    }

    internal IEnumerable<SecurityKey> ResolveSigningKey(string token, SecurityToken securityToken, string keyId, TokenValidationParameters validationParameters)
    {
        if (ResolvedSecurityKeysById.TryGetValue(keyId, out SecurityKey? resolved))
        {
            return [resolved];
        }

        JsonWebKeySet? keySet = FetchKeySetAsync(default).GetAwaiter().GetResult();

        if (keySet != null)
        {
            foreach (JsonWebKey key in keySet.Keys)
            {
                ResolvedSecurityKeysById[key.Kid] = key;
            }
        }

        if (ResolvedSecurityKeysById.TryGetValue(keyId, out resolved))
        {
            return [resolved];
        }

        return [];
    }

    internal async Task<JsonWebKeySet?> FetchKeySetAsync(CancellationToken cancellationToken)
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, _authority);
        requestMessage.Headers.Accept.Add(_acceptHeader);

        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonWebKeySet.Create(result);
    }
}
