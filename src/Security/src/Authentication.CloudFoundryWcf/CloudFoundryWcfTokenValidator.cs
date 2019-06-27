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
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryWcfTokenValidator : CloudFoundryTokenValidator
    {
        public CloudFoundryOptions Options { get; internal protected set; }

        private static ILogger<CloudFoundryTokenValidator> _logger;
        private JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();

        public CloudFoundryWcfTokenValidator(CloudFoundryOptions options, ILogger<CloudFoundryTokenValidator> logger = null)
            : base(options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// <para>Throws a WebFaultException with HTTP 401. exceptionMessage used for Detail if provided, else uses message</para>
        /// Sets WWW-Authenticate value = 'Bearer realm="default",error="{message}",error_description="{exceptionMessage}'"
        /// </summary>
        /// <param name="exceptionMessage">Falls back to value of message if null or empty</param>
        /// <param name="message">Uses value "invalid_token" if not provided</param>
        public static void ThrowJwtException(string exceptionMessage, string message)
        {
            _logger?.LogError("Encountered JWT-related exception: " + exceptionMessage + " " + message);
            if (WebOperationContext.Current != null)
            {
                var headers = WebOperationContext.Current.OutgoingResponse.Headers;

                // https://tools.ietf.org/html/rfc6750#section-3  - "WWW-Authenticate", "Bearer error=\"insufficient_scope\"");
                if (string.IsNullOrEmpty(message))
                {
                    message = "invalid_token";
                }

                if (string.IsNullOrEmpty(exceptionMessage))
                {
                    exceptionMessage = message;
                }

                headers.Add(HttpResponseHeader.WwwAuthenticate, string.Format("Bearer realm=\"default\",error=\"{0}\",error_description=\"{1}\"", message, Regex.Replace(exceptionMessage, @"\s+", " ")));
            }

            WebOperationContext ctx = WebOperationContext.Current;
            if (ctx != null)
            {
                ctx.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
            }

            throw new WebFaultException<string>(exceptionMessage ?? message, HttpStatusCode.Unauthorized);
        }

        public virtual ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            SecurityToken validatedToken = null;
            ClaimsPrincipal principal = null;
            JwtSecurityToken validJwt = null;

            try
            {
                principal = _handler.ValidateToken(token, Options.TokenValidationParameters, out validatedToken);
                validJwt = validatedToken as JwtSecurityToken;
            }
            catch (Exception ex)
            {
                _logger?.LogError("ValidateToken failed: " + ex.Message);
                ThrowJwtException(ex.Message, "invalid_token");
            }

            if (validJwt == null || principal == null)
            {
                ThrowJwtException(null, "invalid_token");
            }

// an exception has already been thrown if principal is null
#pragma warning disable S2259 // Null pointers should not be dereferenced
            CloudFoundryJwt.OnTokenValidatedAddClaims((ClaimsIdentity)principal.Identity, validJwt);
#pragma warning restore S2259 // Null pointers should not be dereferenced

            bool validScopes = ValidateScopes(validJwt);
            if (!validScopes)
            {
                ThrowJwtException(null, "insufficient_scope");
            }

            return principal;
        }
    }
}