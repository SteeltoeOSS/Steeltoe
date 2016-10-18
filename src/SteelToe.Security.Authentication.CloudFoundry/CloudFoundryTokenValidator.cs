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

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryTokenValidator
    {
        public const string ACCESS_TOKEN_KEY = ".Token.access_token";

        public CloudFoundryOptions Options { get; internal protected set; }

        private JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();

        public CloudFoundryTokenValidator(CloudFoundryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            Options = options;

        }

        public virtual string ValidateIssuer(string issuer, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (issuer.Contains("uaa"))
            {
                return issuer;
            }
            return null;
        }

        public virtual bool ValidateAudience(IEnumerable<string> audiences, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            foreach(string audience in audiences)
            {
                if (audience.Equals(Options.ClientId))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual async Task ValidateCookieAsync(CookieValidatePrincipalContext context)
        {

            string token = GetAccessToken(context.Properties.Items);
            bool result = ValidateToken(token);

            if (!result)
            {
                context.RejectPrincipal();
                await context.HttpContext.Authentication.SignOutAsync(CloudFoundryOptions.AUTHENTICATION_SCHEME);
            }
        }

        public virtual bool ValidateToken(string token)
        {
            SecurityToken validatedToken = null;
            ClaimsPrincipal principal = null;
            JwtSecurityToken validJwt = null;
            try
            {

                if (!string.IsNullOrEmpty(token))
                {
                    principal = _handler.ValidateToken(token, Options.TokenValidationParameters, out validatedToken);
                    validJwt = validatedToken as JwtSecurityToken;
                }

            } catch (Exception)
            {
                //LOG
            }
            if (validJwt == null || principal == null)
            {
                return false;
            }
            return true;
        }
        
        public virtual string GetAccessToken(IDictionary<string, string> items)
        {
            string result = null;
            items.TryGetValue(ACCESS_TOKEN_KEY, out result);
            return result;
        }
    }
}
