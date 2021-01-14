// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryHelper
    {
        private static readonly DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static List<string> GetScopes(JsonElement user)
        {
            var result = new List<string>();
            var scopes = user.GetProperty("scope");

            if (scopes.ValueKind is JsonValueKind.Array)
            {
                foreach (var value in scopes.EnumerateArray())
                {
                    result.Add(value.GetString());
                }

                return result;
            }

            return result;
        }

        public static HttpMessageHandler GetBackChannelHandler(bool validateCertificates)
        {
            if (!validateCertificates)
            {
                var handler = new HttpClientHandler
                {
#pragma warning disable S4830 // Server certificates should be verified during SSL/TLS connections
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
#pragma warning restore S4830 // Server certificates should be verified during SSL/TLS connections
                };
                return handler;
            }

            return null;
        }

        public static TokenValidationParameters GetTokenValidationParameters(TokenValidationParameters parameters, string keyUrl, HttpMessageHandler handler, bool validateCertificates, AuthServerOptions options = null)
        {
            if (parameters == null)
            {
                parameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateLifetime = true
                };
            }

            var tokenValidator = new CloudFoundryTokenValidator(options ?? new AuthServerOptions());
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
        public static DateTime GetIssueTime(JsonElement payload)
        {
            var time = payload.GetProperty("iat").GetInt64();
            return ToAbsoluteUTC(time);
        }

        /// <summary>
        /// Retrieves expiration time property (exp) in a <see cref="JsonDocument"/>
        /// </summary>
        /// <param name="payload">Contents of a JWT</param>
        /// <returns>The <see cref="DateTime"/> representation of a token's expiration</returns>
        public static DateTime GetExpTime(JsonElement payload)
        {
            var time = payload.GetProperty("exp").GetInt64();
            return ToAbsoluteUTC(time);
        }

        private static DateTime ToAbsoluteUTC(long secondsPastEpoch)
        {
            return BaseTime.AddSeconds(secondsPastEpoch);
        }
    }
}
