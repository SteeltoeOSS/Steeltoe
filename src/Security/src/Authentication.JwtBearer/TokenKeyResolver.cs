// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Tokens;

namespace Steeltoe.Security.Authentication.JwtBearer;

internal sealed class TokenKeyResolver
{
    private static readonly MediaTypeWithQualityHeaderValue AcceptHeader = new("application/json");
    private readonly HttpClient _httpClient;
    private readonly Uri _authorityUri;

    internal static ConcurrentDictionary<string, SecurityKey> ResolvedSecurityKeysById { get; } = new();

    public TokenKeyResolver(string authority, HttpClient httpClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentNullException.ThrowIfNull(httpClient);

        if (!authority.EndsWith('/'))
        {
            authority += '/';
        }

        _authorityUri = new Uri($"{authority}token_keys");
        _httpClient = httpClient;
    }

    internal SecurityKey[] ResolveSigningKey(string token, SecurityToken securityToken, string keyId, TokenValidationParameters validationParameters)
    {
        if (ResolvedSecurityKeysById.TryGetValue(keyId, out SecurityKey? resolved))
        {
            return [resolved];
        }

        // can't be async all the way until updates are complete in Microsoft libraries
        // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/468
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
        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, _authorityUri);
        requestMessage.Headers.Accept.Add(AcceptHeader);

        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string result = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonWebKeySet.Create(result);
    }
}
