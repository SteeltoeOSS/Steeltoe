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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryClientTokenResolver
    {
        public CloudFoundryOptions Options { get; internal protected set; }

        public CloudFoundryClientTokenResolver(CloudFoundryOptions options)
        {
            Options = options ?? throw new ArgumentNullException("options null");
        }

        public virtual async Task<string> GetAccessToken()
        {
            HttpRequestMessage requestMessage = GetTokenRequestMessage();

            RemoteCertificateValidationCallback prevValidator = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            HttpClient client = new HttpClient();

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(requestMessage);
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting  access token:" + ex.Message);
            }
            finally
            {
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
            }

            if (response.IsSuccessStatusCode)
            {
                var resp = await response.Content.ReadAsStringAsync();
                var payload = JObject.Parse(resp);

                return payload.Value<string>("access_token");
            }
            else
            {
                var error = "OAuth token endpoint failure: " + await Display(response);
                throw new Exception(error);
            }
        }

        protected internal virtual HttpRequestMessage GetTokenRequestMessage()
        {
            var tokenRequestParameters = GetTokenRequestParameters();

            var requestContent = new FormUrlEncodedContent(tokenRequestParameters);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, Options.AccessTokenEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", GetEncoded(Options.ClientId, Options.ClientSecret));
            requestMessage.Content = requestContent;
            return requestMessage;
        }

        protected internal virtual Dictionary<string, string> GetTokenRequestParameters()
        {
            return new Dictionary<string, string>()
            {
                { "client_id", Options.ClientId },
                { "client_secret", Options.ClientSecret },
                { "response_type", "token" },
                { "grant_type", "client_credentials" },
                {
                    "scope",  Options.RequiredScopes == null ? "openid" :
                                        string.Join(" ", Options.RequiredScopes)
                },
            };
        }

        protected internal string GetEncoded(string user, string password)
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

        private static async Task<string> Display(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            output.Append("Status: " + response.StatusCode + ";");
            output.Append("Headers: " + response.Headers.ToString() + ";");
            output.Append("Body: " + await response.Content.ReadAsStringAsync() + ";");
            return output.ToString();
        }
    }
}