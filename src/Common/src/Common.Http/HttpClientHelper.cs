// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http
{
    public static class HttpClientHelper
    {
        internal static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> _reflectedDelegate = null;

        private const int DEFAULT_GETACCESSTOKEN_TIMEOUT = 10000; // Milliseconds
        private const bool DEFAULT_VALIDATE_CERTIFICATES = true;

        private static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> DefaultDelegate { get; } = (sender, cert, chain, sslPolicyErrors) => true;

        public static HttpClient GetHttpClient(bool validateCertificates, int timeout)
        {
            return GetHttpClient(validateCertificates, null, timeout);
        }

        public static HttpClient GetHttpClient(bool validateCertificates, HttpClientHandler handler, int timeout)
        {
            HttpClient client = null;
            if (Platform.IsFullFramework)
            {
                client = handler == null ? new HttpClient() : new HttpClient(handler);
            }
            else
            {
                if (!validateCertificates)
                {
                    if (handler == null)
                    {
                        handler = new HttpClientHandler();
                    }

                    handler.ServerCertificateCustomValidationCallback = GetDisableDelegate();
                    handler.SslProtocols = SslProtocols.Tls12;
                    client = new HttpClient(handler);
                }
                else
                {
                    client = handler == null ? new HttpClient() : new HttpClient(handler);
                }
            }

            client.Timeout = TimeSpan.FromMilliseconds(timeout);
            return client;
        }

        public static void ConfigureCertificateValidation(
            bool validateCertificates,
            out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator)
        {
            prevValidator = null;
            protocolType = (SecurityProtocolType)0;

            if (Platform.IsFullFramework && !validateCertificates)
            {
                protocolType = ServicePointManager.SecurityProtocol;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;

                // Disabling certificate validation is a bad idea, that's why it's off by default!
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
        }

        [Obsolete("This method has a spelling error and will be removed, use 'ConfigureCertificateValidation'")]
        public static void ConfigureCertificateValidatation(
            bool validateCertificates,
            out SecurityProtocolType protocolType,
            out RemoteCertificateValidationCallback prevValidator)
        {
            ConfigureCertificateValidation(validateCertificates, out protocolType, out prevValidator);
        }

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
            if (user == null)
            {
                user = string.Empty;
            }

            if (password == null)
            {
                password = string.Empty;
            }

            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));
        }

        public static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestUri, Func<string> getAccessToken)
        {
            var request = GetRequestMessage(method, requestUri, null, null);

            if (getAccessToken != null)
            {
                var accessToken = getAccessToken();

                if (accessToken != null)
                {
                    AuthenticationHeaderValue auth = new AuthenticationHeaderValue("Bearer", accessToken);
                    request.Headers.Authorization = auth;
                }
            }

            return request;
        }

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
                AuthenticationHeaderValue auth = new AuthenticationHeaderValue(
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
            ILogger logger = null)
        {
            if (string.IsNullOrEmpty(accessTokenUri))
            {
                throw new ArgumentException(nameof(accessTokenUri));
            }

            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentException(nameof(accessTokenUri));
            }

            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new ArgumentException(nameof(accessTokenUri));
            }

            return GetAccessTokenInternal(accessTokenUri, clientId, clientSecret, timeout, validateCertificates, logger);
        }

        private static async Task<string> GetAccessTokenInternal(
            string accessTokenUri,
            string clientId,
            string clientSecret,
            int timeout,
            bool validateCertificates,
            ILogger logger)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri);
            HttpClient client = GetHttpClient(validateCertificates, timeout);

            // If certificate validation is disabled, inject a callback to handle properly
            HttpClientHelper.ConfigureCertificateValidation(validateCertificates, out SecurityProtocolType prevProtocols, out RemoteCertificateValidationCallback prevValidator);

            AuthenticationHeaderValue auth = new AuthenticationHeaderValue("Basic", GetEncodedUserPassword(clientId, clientSecret));
            request.Headers.Authorization = auth;

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" }
            });

            try
            {
                using (client)
                {
                    using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            logger?.LogInformation(
                                "GetAccessToken returned status: {0} while obtaining access token from: {1}",
                                response.StatusCode,
                                accessTokenUri);
                            return null;
                        }

                        var payload = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        var token = payload.Value<string>("access_token");
                        return token;
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError("GetAccessToken exception: {0} ,obtaining access token from: {1}", e, WebUtility.UrlEncode(accessTokenUri));
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(validateCertificates, prevProtocols, prevValidator);
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
