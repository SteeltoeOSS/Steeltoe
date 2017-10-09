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

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace Steeltoe.Common.Http
{
    public static class HttpClientHelper
    {
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
    }
}
