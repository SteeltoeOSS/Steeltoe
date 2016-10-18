//
// Copyright 2015 the original author or authors.
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

using Microsoft.IdentityModel.Tokens;
using System;
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
        public CloudFoundryOptions Options { get; internal protected set; }

        public Dictionary<string, SecurityKey> Resolved { get; internal protected set; }

        public CloudFoundryTokenKeyResolver(CloudFoundryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = options;
            Resolved = new Dictionary<string, SecurityKey>();
        }

        public virtual IEnumerable<SecurityKey> ResolveSigningKey(string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
        {
            SecurityKey resolved = null;
            if (Resolved.TryGetValue(kid, out resolved))
            {
                return new List<SecurityKey> { resolved };
            }

            JsonWebKeySet keyset = FetchKeySet().GetAwaiter().GetResult();
            if (keyset != null)
            {
                foreach(JsonWebKey key in keyset.Keys)
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

#if NET451
            
            byte[] existing = Base64UrlEncoder.DecodeBytes(key.N);
            TrimKey(key, existing);
#endif
            return key;
        }

#if NET451
        private void TrimKey(JsonWebKey key, byte[] existing)
        {
            byte[] signRemoved = new byte[existing.Length -1];
            Buffer.BlockCopy(existing, 1, signRemoved, 0, existing.Length - 1);
            string withSignRemoved = Base64UrlEncoder.Encode(signRemoved);
            key.N = withSignRemoved;
        }
#endif

        public virtual async Task<JsonWebKeySet> FetchKeySet()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, Options.JwtKeyUrl);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpClient client = GetHttpClient();
#if NET451
            RemoteCertificateValidationCallback prevValidator = null;
            if (!Options.ValidateCertificates)
            {
                prevValidator = ServicePointManager.ServerCertificateValidationCallback;
                ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            }
#endif
            HttpResponseMessage response = null;
            try
            {
                response = await client.SendAsync(requestMessage);
            }
            finally
            {
#if NET451
                ServicePointManager.ServerCertificateValidationCallback = prevValidator;
#endif
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
            if (Options.BackchannelHttpHandler != null)
            {
                return new HttpClient(Options.BackchannelHttpHandler);
            }
            return new HttpClient();
        }
    }
}
