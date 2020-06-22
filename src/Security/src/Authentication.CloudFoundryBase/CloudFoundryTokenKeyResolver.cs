﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            JsonWebKeySet keyset = FetchKeySet().GetAwaiter().GetResult();
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
                response = await client.SendAsync(requestMessage).ConfigureAwait(false);
            }
            finally
            {
                HttpClientHelper.RestoreCertificateValidation(_validateCertificates, prevProtocols, prevValidator);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
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
