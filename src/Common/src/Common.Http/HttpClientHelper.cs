// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http
{
    public static class HttpClientHelper
    {
        public static string SteeltoeUserAgent { get; } = $"Steeltoe/{typeof(HttpClientHelper).Assembly.GetName().Version}";

        private const int DEFAULT_GETACCESSTOKEN_TIMEOUT = 10000; // Milliseconds
        private const bool DEFAULT_VALIDATE_CERTIFICATES = true;

        private static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _reflectedDelegate;

        private static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> DefaultDelegate { get; } = (sender, cert, chain, sslPolicyErrors) => true;

        /// <summary>
        /// Gets an HttpClient with user agent <see cref="SteeltoeUserAgent"/>
        /// </summary>
        /// <param name="validateCertificates">Whether or not remote certificates should be validated</param>
        /// <param name="timeoutMillis">Timeout in milliseconds</param>
        public static HttpClient GetHttpClient(bool validateCertificates, int timeoutMillis)
        {
            return GetHttpClient(validateCertificates, null, timeoutMillis);
        }

        /// <summary>
        /// Gets an HttpClient with user agent <see cref="SteeltoeUserAgent"/>
        /// </summary>
        /// <param name="validateCertificates">Whether or not remote certificates should be validated</param>
        /// <param name="handler">A pre-defined <see cref="HttpClientHandler"/></param>
        /// <param name="timeoutMillis">Timeout in milliseconds</param>
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
        /// Gets an HttpClient with user agent <see cref="SteeltoeUserAgent"/>
        /// </summary>
        /// <param name="handler">A pre-defined <see cref="HttpMessageHandler"/></param>
        /// <param name="timeoutMillis">Timeout in milliseconds</param>
        public static HttpClient GetHttpClient(HttpMessageHandler handler, int timeoutMillis = 1500)
        {
            var client = handler == null ? new HttpClient() : new HttpClient(handler);
            client.Timeout = TimeSpan.FromMilliseconds(timeoutMillis);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(SteeltoeUserAgent);
            return client;
        }

        /// <summary>
        /// Disable certificate validation on demand. Has no effect unless <see cref="Platform.IsFullFramework"/>
        /// </summary>
        /// <param name="validateCertificates">Whether or not certificates should be validated</param>
        /// <param name="protocolType"><see cref="SecurityProtocolType"/></param>
        /// <param name="prevValidator">Pre-existing certificate validation callback</param>
        public static void ConfigureCertificateValidation(
            bool validateCertificates,
            out SecurityProtocolType protocolType,
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
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
            }
        }

        /// <summary>
        /// Returns certificate validation to its original state. Has no effect unless <see cref="Platform.IsFullFramework"/>
        /// </summary>
        /// <param name="validateCertificates">Whether or not certificates should be validated</param>
        /// <param name="protocolType"><see cref="SecurityProtocolType"/></param>
        /// <param name="prevValidator">Pre-existing certificate validation callback</param>
        public static void RestoreCertificateValidation(
            bool validateCertificates,
            SecurityProtocolType protocolType,
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
        /// Creates an <see cref="HttpRequestMessage" /> from the provided information
        /// </summary>
        /// <param name="method"><see cref="HttpMethod"/></param>
        /// <param name="requestUri">The remote Uri</param>
        /// <param name="getAccessToken">A means of including a bearer token</param>
        public static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestUri, Func<string> getAccessToken)
        {
            var request = GetRequestMessage(method, requestUri, null, null);

            if (getAccessToken != null)
            {
                var accessToken = getAccessToken();

                if (accessToken != null)
                {
                    var auth = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.Authorization = auth;
                }
            }

            return request;
        }

        /// <summary>
        /// Creates an <see cref="HttpRequestMessage" /> from the provided information
        /// </summary>
        /// <param name="method"><see cref="HttpMethod"/></param>
        /// <param name="requestUri">The remote Uri</param>
        /// <param name="userName">Optional Basic Auth Username. Not used unless password is not null or empty</param>
        /// <param name="password">Optional Basic Auth Password</param>
        public static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestUri, string userName, string password)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (requestUri == null)
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            var request = new HttpRequestMessage(method, requestUri);
            if (!string.IsNullOrEmpty(password))
            {
                var auth = new AuthenticationHeaderValue(
                    "Basic",
                    GetEncodedUserPassword(userName, password));
                request.Headers.Authorization = auth;
            }

            return request;
        }

        public static Task<string> GetAccessToken(
            string accessTokenUri,
            string clientId,
            string clientSecret,
            int timeout = DEFAULT_GETACCESSTOKEN_TIMEOUT,
            bool validateCertificates = DEFAULT_VALIDATE_CERTIFICATES,
            HttpClient httpClient = null,
            ILogger logger = null)
        {
            if (string.IsNullOrEmpty(accessTokenUri))
            {
                throw new ArgumentException(nameof(accessTokenUri));
            }

            var parsedUri = new Uri(accessTokenUri);
            return GetAccessToken(parsedUri, clientId, clientSecret, timeout, validateCertificates, null, httpClient, logger);
        }

        public static Task<string> GetAccessToken(
            Uri accessTokenUri,
            string clientId,
            string clientSecret,
            int timeout = DEFAULT_GETACCESSTOKEN_TIMEOUT,
            bool validateCertificates = DEFAULT_VALIDATE_CERTIFICATES,
            Dictionary<string, string> additionalParams = null,
            HttpClient httpClient = null,
            ILogger logger = null)
        {
            if (accessTokenUri is null)
            {
                throw new ArgumentException(nameof(accessTokenUri));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentException(nameof(clientId));
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentException(nameof(clientSecret));
            }

            if (!accessTokenUri.IsWellFormedOriginalString())
            {
                throw new ArgumentException("Access token Uri is not well formed", nameof(accessTokenUri));
            }

            return GetAccessTokenInternal(accessTokenUri, clientId, clientSecret, timeout, validateCertificates, httpClient, additionalParams, logger);
        }

        private static async Task<string> GetAccessTokenInternal(
            Uri accessTokenUri,
            string clientId,
            string clientSecret,
            int timeout,
            bool validateCertificates,
            HttpClient httpClient,
            Dictionary<string, string> additionalParams,
            ILogger logger)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri);
            logger?.LogInformation("HttpClient not provided, a new instance will be created and disposed after retrieving a token");
            var client = httpClient ?? GetHttpClient(validateCertificates, timeout);

            // If certificate validation is disabled, inject a callback to handle properly
            ConfigureCertificateValidation(validateCertificates, out var prevProtocols, out var prevValidator);

            var auth = new AuthenticationHeaderValue("Basic", GetEncodedUserPassword(clientId, clientSecret));
            request.Headers.Authorization = auth;

            var reqparams = additionalParams is null
                ? new Dictionary<string, string> { { "grant_type", "client_credentials" } }
                : new Dictionary<string, string>(additionalParams) { { "grant_type", "client_credentials" } };

            request.Content = new FormUrlEncodedContent(reqparams);

            try
            {
                using var response = await client.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    logger?.LogInformation(
                        "GetAccessToken returned status: {0} while obtaining access token from: {1}",
                        response.StatusCode,
                        WebUtility.UrlEncode(accessTokenUri.OriginalString));
                    return null;
                }

                var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var token = payload.RootElement.EnumerateObject().FirstOrDefault(n => n.Name.Equals("access_token")).Value;

                if (httpClient is null)
                {
                    client.Dispose();
                }

                return token.ToString();
            }
            catch (Exception e)
            {
                logger?.LogError("GetAccessToken exception: {0}, obtaining access token from: {1}", e, WebUtility.UrlEncode(accessTokenUri.OriginalString));
            }
            finally
            {
                RestoreCertificateValidation(validateCertificates, prevProtocols, prevValidator);
            }

            return null;
        }

#pragma warning disable SA1202 // Elements must be ordered by access
        internal static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> GetDisableDelegate()
#pragma warning restore SA1202 // Elements must be ordered by access
        {
            if (Platform.IsFullFramework)
            {
                return null;
            }

            if (_reflectedDelegate != null)
            {
                return _reflectedDelegate;
            }

            var property = typeof(HttpClientHandler).GetProperty(
                "DangerousAcceptAnyServerCertificateValidator",
                BindingFlags.Public | BindingFlags.Static);

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
}
