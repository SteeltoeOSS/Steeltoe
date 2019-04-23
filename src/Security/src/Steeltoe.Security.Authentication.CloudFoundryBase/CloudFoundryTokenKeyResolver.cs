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

using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryTokenKeyResolver
    {
        internal static ConcurrentDictionary<string, SecurityKey> Resolved { get; set; } = new ConcurrentDictionary<string, SecurityKey>();

        private readonly string _jwtKeyUrl;
        private readonly HttpMessageHandler _httpHandler;
        private readonly bool _validateCertificates;

        public CloudFoundryTokenKeyResolver(string jwtKeyUrl, HttpMessageHandler httpHandler, bool validateCertificates)
        {
            if (string.IsNullOrEmpty(jwtKeyUrl))
            {
                throw new ArgumentException(nameof(jwtKeyUrl));
            }

            _jwtKeyUrl = jwtKeyUrl;
            _httpHandler = httpHandler;
            _validateCertificates = validateCertificates;
        }

        public virtual IEnumerable<SecurityKey> ResolveSigningKey(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
        {
            if (Resolved.TryGetValue(kid, out SecurityKey resolved))
            {
                return new List<SecurityKey> { resolved };
            }

            JsonWebKeySet keyset = Task.Run(() => FetchKeySet()).GetAwaiter().GetResult();
            if (keyset != null)
            {
                foreach (JsonWebKey key in keyset.Keys)
                {
                    FixupKey(key);
                    Resolved[key.Kid] = key;
                }
            }

            if (Resolved.TryGetValue(kid, out resolved))
            {
                return new List<SecurityKey> { resolved };
            }

            return null;
        }

        public JsonWebKey FixupKey(JsonWebKey key)
        {
            if (Platform.IsFullFramework)
            {
                byte[] existing = Base64UrlEncoder.DecodeBytes(key.N);
                TrimKey(key, existing);
            }

            return key;
        }

        public virtual async Task<JsonWebKeySet> FetchKeySet()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, _jwtKeyUrl);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpClient client = GetHttpClient();

            HttpClientHelper.ConfigureCertificateValidation(
                _validateCertificates,
                out SecurityProtocolType prevProtocols,
                out RemoteCertificateValidationCallback prevValidator);

            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(requestMessage);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, prevProtocols, prevValidator);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return GetJsonWebKeySet(result);
            }

            return null;
        }

        public virtual JsonWebKeySet GetJsonWebKeySet(string json)
        {
            return JsonWebKeySet.Create(json);
        }

        public virtual HttpClient GetHttpClient()
        {
            if (_httpHandler != null)
            {
                return new HttpClient(_httpHandler);
            }

            return new HttpClient();
        }

        private void TrimKey(JsonWebKey key, byte[] existing)
        {
            byte[] signRemoved = new byte[existing.Length - 1];
            Buffer.BlockCopy(existing, 1, signRemoved, 0, existing.Length - 1);
            string withSignRemoved = Base64UrlEncoder.Encode(signRemoved);
            key.N = withSignRemoved;
        }
    }
}
