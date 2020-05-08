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
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryTokenValidator
    {
        private readonly AuthServerOptions _options;

        public CloudFoundryTokenValidator(AuthServerOptions options = null)
        {
            _options = options ?? new AuthServerOptions();
        }

        /// <summary>
        /// Validate that a token was issued by UAA
        /// </summary>
        /// <param name="issuer">Token issuer</param>
        /// <param name="securityToken">[Not used] Token to validate</param>
        /// <param name="validationParameters">[Not used]</param>
        /// <returns>The issuer, if valid, else <see langword="null" /></returns>
        public virtual string ValidateIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (issuer.Contains("uaa"))
            {
                return issuer;
            }

            return null;
        }

        /// <summary>
        /// Validate that a token was meant for approved audience(s)
        /// </summary>
        /// <param name="audiences">The list of audiences the token is valid for</param>
        /// <param name="securityToken">[Not used] The token being validated</param>
        /// <param name="validationParameters">[Not used]</param>
        /// <returns><see langword="true"/> if the audience matches the client id or any value in AdditionalAudiences</returns>
        public virtual bool ValidateAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            foreach (var audience in audiences)
            {
                if (audience.Equals(_options.ClientId))
                {
                    return true;
                }

                if (_options.AdditionalAudiences != null)
                {
                    var found = _options.AdditionalAudiences.Any(x => x.Equals(audience));
                    if (found)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// This method validates scopes provided in configuration,
        /// to perform scope based Authorization
        /// </summary>
        /// <param name="validJwt">JSON Web token</param>
        /// <returns>true if scopes validated</returns>
        protected virtual bool ValidateScopes(JwtSecurityToken validJwt)
        {
            if (_options.RequiredScopes == null || !_options.RequiredScopes.Any())
            {
                return true; // nocheck
            }

            if (!validJwt.Claims.Any(x => x.Type.Equals("scope") || x.Type.Equals("authorities")))
            {
                return false; // no scopes at all
            }

            var found = false;
            foreach (var claim in validJwt.Claims)
            {
                if (claim.Type.Equals("scope") || (claim.Type.Equals("authorities") && _options.RequiredScopes.Any(x => x.Equals(claim.Value))))
                {
                    return true;
                }
            }

            return found;
        }
    }
}
