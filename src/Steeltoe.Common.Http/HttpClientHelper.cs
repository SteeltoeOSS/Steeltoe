//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http
{
    public static class HttpClientHelper
    {
        private const int DEFAULT_GETACCESSTOKEN_TIMEOUT = 10000; // Milliseconds
        private const bool DEFAULT_VALIDATE_CERTIFICATES = true;


        public static HttpClient GetHttpClient(bool validateCertificates, int timeout)
        {
            HttpClient client = null;
            if (Platform.IsFullFramework)
            {
                client = new HttpClient();
            }
            else
            {

                if (!validateCertificates)
                {
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                    handler.SslProtocols = SslProtocols.Tls12;
                    client = new HttpClient(handler);
                }
                else
                {
                    client = new HttpClient();
                }
            }
            client.Timeout = TimeSpan.FromMilliseconds(timeout);
            return client;
        }
        public static void ConfigureCertificateValidatation(bool validateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator)
        {
            prevValidator = null;
            protocolType = (SecurityProtocolType)0;

            if (Platform.IsFullFramework)
            {
                if (!validateCertificates)
                {
                    protocolType = ServicePointManager.SecurityProtocol;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                    ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                }
            }
        }
        public static void RestoreCertificateValidation(bool validateCertificates, SecurityProtocolType protocolType, RemoteCertificateValidationCallback prevValidator)
        {

            if (Platform.IsFullFramework)
            {
                if (!validateCertificates)
                {
                    ServicePointManager.SecurityProtocol = protocolType;
                    ServicePointManager.ServerCertificateValidationCallback = prevValidator;
                }

            }

        }
        public static string GetEncodedUserPassword(string user, string password)
        {
            if (user == null)
                user = string.Empty;
            if (password == null)
                password = string.Empty;
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + password));
        }

        public static HttpRequestMessage GetRequestMessage(HttpMethod method, string requestUri, Func<string> GetAccessToken)
        {
            var request = GetRequestMessage(method, requestUri, null, null);

            if (GetAccessToken != null)
            {
                var accessToken = GetAccessToken();

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
                AuthenticationHeaderValue auth = new AuthenticationHeaderValue("Basic",
                    GetEncodedUserPassword(userName, password));
                request.Headers.Authorization = auth;
            }

            return request;
        }
        public static async Task<string> GetAccessToken(
            string accessTokenUri, string clientId, string clientSecret, 
            int timeout = DEFAULT_GETACCESSTOKEN_TIMEOUT, bool validateCertificates = DEFAULT_VALIDATE_CERTIFICATES, ILogger logger = null)
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
            var request = new HttpRequestMessage(HttpMethod.Post, accessTokenUri);
            HttpClient client = GetHttpClient(validateCertificates, timeout);

            // If certificate validation is disabled, inject a callback to handle properly
            RemoteCertificateValidationCallback prevValidator = null;
            SecurityProtocolType prevProtocols = (SecurityProtocolType)0;
            HttpClientHelper.ConfigureCertificateValidatation(validateCertificates, out prevProtocols, out prevValidator);

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
                            logger?.LogInformation("GetAccessToken returned status: {0} while obtaining access token from: {1}",
                                response.StatusCode, accessTokenUri);
                            return null;
                        }

                        var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                        var token = payload.Value<string>("access_token");
                        return token;
                    }
                }
            }
            catch (Exception e)
            {
                logger?.LogError("GetAccessToken exception: {0} ,obtaining access token from: {1}", e, accessTokenUri);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(validateCertificates, prevProtocols, prevValidator);
            }
            return null;
        }
    }
}
