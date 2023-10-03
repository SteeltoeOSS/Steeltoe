// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Http;

public static class HttpClientHelper
{
    internal const int DefaultGetAccessTokenTimeout = 10000; // Milliseconds
    internal const bool DefaultValidateCertificates = true;

    public static string SteeltoeUserAgent { get; } = $"Steeltoe/{typeof(HttpClientHelper).Assembly.GetName().Version}";

    /// <summary>
    /// Gets an HttpClient with user agent <see cref="SteeltoeUserAgent" />.
    /// </summary>
    /// <param name="validateCertificates">
    /// Whether or not remote certificates should be validated.
    /// </param>
    /// <param name="timeoutMillis">
    /// Timeout in milliseconds.
    /// </param>
    public static HttpClient GetHttpClient(bool validateCertificates, int timeoutMillis)
    {
        return GetHttpClient(validateCertificates, null, timeoutMillis);
    }

    /// <summary>
    /// Gets an HttpClient with user agent <see cref="SteeltoeUserAgent" />.
    /// </summary>
    /// <param name="validateCertificates">
    /// Whether or not remote certificates should be validated.
    /// </param>
    /// <param name="handler">
    /// A pre-defined <see cref="HttpClientHandler" />.
    /// </param>
    /// <param name="timeoutMillis">
    /// Timeout in milliseconds.
    /// </param>
    public static HttpClient GetHttpClient(bool validateCertificates, HttpClientHandler handler, int timeoutMillis)
    {
        HttpClient client;

        if (!validateCertificates)
        {
            handler ??= new HttpClientHandler();
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
            handler.SslProtocols = SslProtocols.Tls12;
            client = new HttpClient(handler);
        }
        else
        {
            client = handler == null ? new HttpClient() : new HttpClient(handler);
        }

        client.Timeout = TimeSpan.FromMilliseconds(timeoutMillis);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(SteeltoeUserAgent);
        return client;
    }

    /// <summary>
    /// Gets an HttpClient with user agent <see cref="SteeltoeUserAgent" />.
    /// </summary>
    /// <param name="handler">
    /// A pre-defined <see cref="HttpMessageHandler" />.
    /// </param>
    /// <param name="timeoutMillis">
    /// Timeout in milliseconds.
    /// </param>
    public static HttpClient GetHttpClient(HttpMessageHandler handler, int timeoutMillis = 1500)
    {
        HttpClient client = handler == null ? new HttpClient() : new HttpClient(handler);
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMillis);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(SteeltoeUserAgent);
        return client;
    }

    public static string GetEncodedUserPassword(string user, string password)
    {
        user ??= string.Empty;
        password ??= string.Empty;

        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{password}"));
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage" /> from the provided information.
    /// </summary>
    /// <param name="method">
    /// <see cref="HttpMethod" />.
    /// </param>
    /// <param name="requestUri">
    /// The remote Uri.
    /// </param>
    /// <param name="getAccessTokenAsync">
    /// An async callback to obtain a bearer token.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public static async Task<HttpRequestMessage> GetRequestMessageAsync(HttpMethod method, Uri requestUri,
        Func<CancellationToken, Task<string>> getAccessTokenAsync, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = GetRequestMessage(method, requestUri, null, null);

        if (getAccessTokenAsync != null)
        {
            string accessToken = await getAccessTokenAsync(cancellationToken);

            if (accessToken != null)
            {
                var auth = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Authorization = auth;
            }
        }

        return request;
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage" /> from the provided information.
    /// </summary>
    /// <param name="method">
    /// <see cref="HttpMethod" />.
    /// </param>
    /// <param name="requestUri">
    /// The remote Uri.
    /// </param>
    /// <param name="userName">
    /// Optional Basic Auth Username. Not used unless password is not null or empty.
    /// </param>
    /// <param name="password">
    /// Optional Basic Auth Password.
    /// </param>
    public static HttpRequestMessage GetRequestMessage(HttpMethod method, Uri requestUri, string userName, string password)
    {
        ArgumentGuard.NotNull(method);
        ArgumentGuard.NotNull(requestUri);

        var request = new HttpRequestMessage(method, requestUri);

        if (!string.IsNullOrEmpty(password))
        {
            var auth = new AuthenticationHeaderValue("Basic", GetEncodedUserPassword(userName, password));
            request.Headers.Authorization = auth;
        }

        return request;
    }

    public static Task<string> GetAccessTokenAsync(string accessTokenUri, string clientId, string clientSecret, int timeout, bool validateCertificates,
        HttpClient httpClient, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(accessTokenUri);
        ArgumentGuard.NotNullOrEmpty(clientId);
        ArgumentGuard.NotNullOrEmpty(clientSecret);

        var parsedUri = new Uri(accessTokenUri);

        if (!parsedUri.IsWellFormedOriginalString())
        {
            throw new ArgumentException("Access token Uri is not well-formed.", nameof(accessTokenUri));
        }

        return GetAccessTokenAsync(parsedUri, clientId, clientSecret, timeout, validateCertificates, null, httpClient, logger, cancellationToken);
    }

    private static async Task<string> GetAccessTokenAsync(Uri accessTokenUri, string clientId, string clientSecret, int timeout, bool validateCertificates,
        Dictionary<string, string> additionalParams, HttpClient httpClient, ILogger logger, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri);
        logger?.LogInformation("HttpClient not provided, a new instance will be created and disposed after retrieving a token");
        HttpClient client = httpClient ?? GetHttpClient(validateCertificates, timeout);

        var auth = new AuthenticationHeaderValue("Basic", GetEncodedUserPassword(clientId, clientSecret));
        request.Headers.Authorization = auth;

        Dictionary<string, string> parameters = additionalParams is null
            ? new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            }
            : new Dictionary<string, string>(additionalParams)
            {
                { "grant_type", "client_credentials" }
            };

        request.Content = new FormUrlEncodedContent(parameters);

        try
        {
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger?.LogInformation("GetAccessTokenAsync returned status: {statusCode} while obtaining access token from: {uri}", response.StatusCode,
                    WebUtility.UrlEncode(accessTokenUri.OriginalString));

                return null;
            }

            JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            JsonElement token = payload.RootElement.EnumerateObject().FirstOrDefault(n => n.Name == "access_token").Value;

            return token.ToString();
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            logger?.LogError(exception, "GetAccessTokenAsync exception obtaining access token from: {uri}",
                WebUtility.UrlEncode(accessTokenUri.OriginalString));
        }
        finally
        {
            if (httpClient is null)
            {
                client.Dispose();
            }
        }

        return null;
    }
}
