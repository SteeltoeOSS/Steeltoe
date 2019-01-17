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
using Newtonsoft.Json.Linq;
using Steeltoe.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryHelper
    {
        private static DateTime baseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static List<string> GetScopes(JObject user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            List<string> result = new List<string>();
            var scopes = user["scope"];
            if (scopes == null)
            {
                return result;
            }

            if (scopes is JValue asValue)
            {
                result.Add(asValue.Value<string>());
                return result;
            }

            if (scopes is JArray asArray)
            {
                foreach (string s in asArray)
                {
                    result.Add(s);
                }
            }

            return result;
        }

        public static HttpMessageHandler GetBackChannelHandler(bool validateCertificates)
        {
            if (Platform.IsFullFramework)
            {
                return null;
            }
            else
            {
                if (!validateCertificates)
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                    };
                    return handler;
                }

                return null;
            }
        }

        public static TokenValidationParameters GetTokenValidationParameters(TokenValidationParameters parameters, string keyUrl, HttpMessageHandler handler, bool validateCertificates, AuthServerOptions options = null)
        {
            if (parameters == null)
            {
                parameters = new TokenValidationParameters();
            }

            var tokenValidator = new CloudFoundryTokenValidator(options ?? new AuthServerOptions());
            parameters.ValidateAudience = false;
            parameters.ValidateIssuer = true;
            parameters.ValidateLifetime = true;
            parameters.IssuerValidator = tokenValidator.ValidateIssuer;
            parameters.AudienceValidator = tokenValidator.ValidateAudience;

            var tkr = new CloudFoundryTokenKeyResolver(keyUrl, handler, validateCertificates);
            parameters.IssuerSigningKeyResolver = tkr.ResolveSigningKey;

            return parameters;
        }

        /// <summary>
        /// Retrieves the time at which a token was issued
        /// </summary>
        /// <param name="payload">Contents of a JWT</param>
        /// <returns>The <see cref="DateTime"/> representation of a token's issued-at time</returns>
        public static DateTime GetIssueTime(JObject payload)
            {
                if (payload == null)
                {
                    throw new ArgumentNullException(nameof(payload));
                }

                var time = payload.Value<long>("iat");
                return ToAbsoluteUTC(time);
            }

        /// <summary>
        /// Retrieves expiration time property (exp) in a <see cref="JObject"/>
        /// </summary>
        /// <param name="payload">Contents of a JWT</param>
        /// <returns>The <see cref="DateTime"/> representation of a token's expiration</returns>
        public static DateTime GetExpTime(JObject payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            var time = payload.Value<long>("exp");
            return ToAbsoluteUTC(time);
        }

        private static DateTime ToAbsoluteUTC(long secondsPastEpoch)
        {
            return baseTime.AddSeconds(secondsPastEpoch);
        }
    }
}
