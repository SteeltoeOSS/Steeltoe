// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Steeltoe.Common.Extensions;

namespace Steeltoe.Common.Http;

internal static class HttpClientExtensions
{
    internal static readonly string SteeltoeUserAgent = $"Steeltoe/{typeof(HttpClientExtensions).Assembly.GetName().Version}";

    /// <summary>
    /// Sends an HTTP GET request to obtain an access token.
    /// </summary>
    /// <param name="httpClient">
    /// An unused <see cref="HttpClient" /> instance. Its inner <see cref="HttpClientHandler" /> can be long-lived or pooled. See
    /// https://github.com/dotnet/aspnetcore/issues/10542#issuecomment-603085670.
    /// </param>
    /// <param name="accessTokenUri">
    /// The URI to send the GET request to.
    /// </param>
    /// <param name="username">
    /// The username to authenticate.
    /// </param>
    /// <param name="password">
    /// The password to authenticate.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// The access token.
    /// </returns>
    public static async Task<string> GetAccessTokenAsync(this HttpClient httpClient, Uri accessTokenUri, string? username, string? password,
        CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(httpClient);
        ArgumentGuard.NotNull(accessTokenUri);

        var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri)
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")))
            },
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            })
        };

        httpClient.ConfigureForSteeltoe(null);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseDocument = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken);
        string? accessToken = responseDocument?.AccessToken;

        if (string.IsNullOrEmpty(accessToken))
        {
            throw new HttpRequestException($"No access token was returned from '{accessTokenUri.ToMaskedString()}'.", null, response.StatusCode);
        }

        return accessToken;
    }

    public static void ConfigureForSteeltoe(this HttpClient httpClient, TimeSpan? timeout)
    {
        if (timeout > TimeSpan.Zero)
        {
            httpClient.Timeout = timeout.Value;
        }

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(SteeltoeUserAgent);
    }

    internal sealed class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;
    }
}
