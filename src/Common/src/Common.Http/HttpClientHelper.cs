// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Http;

public static class HttpClientHelper
{
    private const int DefaultGetAccessTokenTimeout = 10000; // Milliseconds
    private const bool DefaultValidateCertificates = true;

    private static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _reflectedDelegate;

    private static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> DefaultDelegate { get; } = (_, _, _, _) => true;

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

        if (Platform.IsFullFramework)
        {
            client = handler == null ? new HttpClient() : new HttpClient(handler);
        }
        else
        {
            if (!validateCertificates)
            {
                handler ??= new HttpClientHandler();

                handler.ServerCertificateCustomValidationCallback = GetDisableDelegate();
                handler.SslProtocols = SslProtocols.Tls12;
                client = new HttpClient(handler);
            }
            else
            {
                client = handler == null ? new HttpClient() : new HttpClient(handler);
            }
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

    /// <summary>
    /// Disable certificate validation on demand. Has no effect unless <see cref="Platform.IsFullFramework" />.
    /// </summary>
    /// <param name="validateCertificates">
    /// Whether or not certificates should be validated.
    /// </param>
    /// <param name="protocolType">
    /// <see cref="SecurityProtocolType" />.
    /// </param>
    /// <param name="prevValidator">
    /// Pre-existing certificate validation callback.
    /// </param>
    public static void ConfigureCertificateValidation(bool validateCertificates, out SecurityProtocolType protocolType,
        out RemoteCertificateValidationCallback prevValidator)
    {
        prevValidator = null;
        protocolType = 0;

        if (Platform.IsFullFramework && !validateCertificates)
        {
            protocolType = ServicePointManager.SecurityProtocol;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            prevValidator = ServicePointManager.ServerCertificateValidationCallback;

            // Disabling certificate validation is a bad idea, that's why it's off by default!
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
            ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
        }
    }

    /// <summary>
    /// Returns certificate validation to its original state. Has no effect unless <see cref="Platform.IsFullFramework" />.
    /// </summary>
    /// <param name="validateCertificates">
    /// Whether or not certificates should be validated.
    /// </param>
    /// <param name="protocolType">
    /// <see cref="SecurityProtocolType" />.
    /// </param>
    /// <param name="prevValidator">
    /// Pre-existing certificate validation callback.
    /// </param>
    public static void RestoreCertificateValidation(bool validateCertificates, SecurityProtocolType protocolType,
        RemoteCertificateValidationCallback prevValidator)
    {
        if (Platform.IsFullFramework && !validateCertificates)
        {
            ServicePointManager.SecurityProtocol = protocolType;
            ServicePointManager.ServerCertificateValidationCallback = prevValidator;
        }
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
    /// <param name="getAccessToken">
    /// A means of including a bearer token.
    /// </param>
    public static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestUri, Func<string> getAccessToken)
    {
        HttpRequestMessage request = GetRequestMessage(method, requestUri, null, null);

        if (getAccessToken != null)
        {
            string accessToken = getAccessToken();

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
    public static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestUri, string userName, string password)
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

    public static Task<string> GetAccessTokenAsync(string accessTokenUri, string clientId, string clientSecret, int timeout = DefaultGetAccessTokenTimeout,
        bool validateCertificates = DefaultValidateCertificates, HttpClient httpClient = null, ILogger logger = null)
    {
        ArgumentGuard.NotNullOrEmpty(accessTokenUri);

        var parsedUri = new Uri(accessTokenUri);
        return GetAccessTokenAsync(parsedUri, clientId, clientSecret, timeout, validateCertificates, null, httpClient, logger);
    }

    public static Task<string> GetAccessTokenAsync(Uri accessTokenUri, string clientId, string clientSecret, int timeout = DefaultGetAccessTokenTimeout,
        bool validateCertificates = DefaultValidateCertificates, Dictionary<string, string> additionalParams = null, HttpClient httpClient = null,
        ILogger logger = null)
    {
        ArgumentGuard.NotNull(accessTokenUri);
        ArgumentGuard.NotNullOrEmpty(clientId);
        ArgumentGuard.NotNullOrEmpty(clientSecret);

        if (!accessTokenUri.IsWellFormedOriginalString())
        {
            throw new ArgumentException("Access token Uri is not well-formed.", nameof(accessTokenUri));
        }

        return GetAccessTokenInternalAsync(accessTokenUri, clientId, clientSecret, timeout, validateCertificates, httpClient, additionalParams, logger);
    }

    private static async Task<string> GetAccessTokenInternalAsync(Uri accessTokenUri, string clientId, string clientSecret, int timeout,
        bool validateCertificates, HttpClient httpClient, Dictionary<string, string> additionalParams, ILogger logger)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri);
        logger?.LogInformation("HttpClient not provided, a new instance will be created and disposed after retrieving a token");
        HttpClient client = httpClient ?? GetHttpClient(validateCertificates, timeout);

        // If certificate validation is disabled, inject a callback to handle properly
        ConfigureCertificateValidation(validateCertificates, out SecurityProtocolType prevProtocols, out RemoteCertificateValidationCallback prevValidator);

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
            using HttpResponseMessage response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                logger?.LogInformation("GetAccessTokenAsync returned status: {statusCode} while obtaining access token from: {uri}", response.StatusCode,
                    WebUtility.UrlEncode(accessTokenUri.OriginalString));

                return null;
            }

            JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement token = payload.RootElement.EnumerateObject().FirstOrDefault(n => n.Name.Equals("access_token")).Value;

            if (httpClient is null)
            {
                client.Dispose();
            }

            return token.ToString();
        }
        catch (Exception e)
        {
            logger?.LogError(e, "GetAccessTokenAsync exception obtaining access token from: {uri}", WebUtility.UrlEncode(accessTokenUri.OriginalString));
        }
        finally
        {
            RestoreCertificateValidation(validateCertificates, prevProtocols, prevValidator);
        }

        return null;
    }

    internal static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> GetDisableDelegate()
    {
        if (Platform.IsFullFramework)
        {
            return null;
        }

        if (_reflectedDelegate != null)
        {
            return _reflectedDelegate;
        }

        PropertyInfo property = typeof(HttpClientHandler).GetProperty(
            "DangerousAcceptAnyServerCertificateValidator", BindingFlags.Public | BindingFlags.Static);

        if (property != null)
        {
            _reflectedDelegate = property.GetValue(null) as Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool>;

            if (_reflectedDelegate != null)
            {
                return _reflectedDelegate;
            }
        }

        return DefaultDelegate;
    }
}
