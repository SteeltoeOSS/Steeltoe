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

using Microsoft.IdentityModel.Tokens;
using Steeltoe.Common.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryTokenKeyResolver
    {
        public CloudFoundryOptions Options { get; internal protected set; }

        public Dictionary<string, SecurityKey> Resolved { get; internal protected set; }

        public CloudFoundryTokenKeyResolver(CloudFoundryOptions options)
        {
            Options = options ?? throw new ArgumentNullException("options null");
            Resolved = new Dictionary<string, SecurityKey>();
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
            byte[] existing = Base64UrlEncoder.DecodeBytes(key.N);
            TrimKey(key, existing);
            return key;
        }

        public virtual async Task<JsonWebKeySet> FetchKeySet()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, Options.JwtKeyEndpoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using (var handler = new HttpClientHandler())
            {
                HttpClientHelper.ConfigureCertificateValidatation(Options.ValidateCertificates, out SecurityProtocolType protocolType, out RemoteCertificateValidationCallback prevValidator);

                HttpClient client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(Options.OAuthServiceUrl)
                };

                HttpResponseMessage response = null;
                try
                {
                    response = await client.SendAsync(requestMessage);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error getting  keys to validate token:" + ex.Message);
                }
                finally
                {
                    HttpClientHelper.RestoreCertificateValidation(Options.ValidateCertificates, protocolType, prevValidator);
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return GetJsonWebKeySet(result);
                }
            }

            return null;
        }

        public virtual JsonWebKeySet GetJsonWebKeySet(string json)
        {
            return new JsonWebKeySetEx(json);
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