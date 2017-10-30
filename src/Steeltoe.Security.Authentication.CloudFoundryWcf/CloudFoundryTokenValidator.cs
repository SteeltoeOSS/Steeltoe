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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.ServiceModel.Web;
using System.Text.RegularExpressions;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryTokenValidator
    {
      
        public CloudFoundryOptions Options { get; internal protected set; }

        private JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();

        public CloudFoundryTokenValidator(CloudFoundryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options null");
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
            foreach (string audience in audiences)
            {
                if (audience.Equals(Options.ClientId))
                {
                    return true;
                }

                if (Options.AdditionalAudiences != null)
                {
                    bool found = Options.AdditionalAudiences.Any(x => x.Equals(audience));
                    if (found)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This method validate scopes provided in configuration, 
        /// to perform scope based Authorization
        /// </summary>
        /// <param name="validJwt"></param>
        /// <returns></returns>
        protected virtual bool ValidateScopes(JwtSecurityToken validJwt)
        {
            
            if (Options.RequiredScopes == null || Options.RequiredScopes.Count<string>() == 0)
                return true; // nocheck

            if (!validJwt.Claims.Any(x => x.Type.Equals("scope") || x.Type.Equals("authorities")))
                return false;// no scopes at all

            bool found = false;
            foreach (Claim claim in validJwt.Claims)
            {
                if (claim.Type.Equals("scope") || claim.Type.Equals("authorities")
                        && Options.RequiredScopes.Any(x => x.Equals(claim.Value)))
                    return true;
            }
            return found;
        }


        public virtual ClaimsPrincipal ValidateToken(string token) 
        {
            if (string.IsNullOrEmpty(token))
                return null;

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
                Console.WriteLine("ValidateToken fails:" + ex.Message);
                throwJwtException(ex.Message, "invalid_token");
            }
            
            if (validJwt == null || principal == null )
                throwJwtException(null, "invalid_token");

            CloudFoundryJwt.OnTokenValidatedAddClaims((ClaimsIdentity)principal.Identity, validJwt);
                  
            bool validScopes = ValidateScopes(validJwt);
            if ( !validScopes)
                throwJwtException(null, "insufficient_scope");
    
            return principal;
        }

       
        public static void throwJwtException (string exeptionMessage, string message)
        {
            Console.Out.WriteLine("error: " + exeptionMessage + " " + message );
            if (WebOperationContext.Current != null)
            {
                var headers = WebOperationContext.Current.OutgoingResponse.Headers;

                //https://tools.ietf.org/html/rfc6750  - "WWW-Authenticate", "Bearer error=\"insufficient_scope\"");
                if (string.IsNullOrEmpty(message))
                    message = "invalid_token";

                if (string.IsNullOrEmpty(exeptionMessage))
                    exeptionMessage = message;

                headers.Add(HttpResponseHeader.WwwAuthenticate, string.Format("Bearer realm=\"default\",error=\"{0}\",error_description=\"{1}\"", message, Regex.Replace(exeptionMessage, @"\s+", " ")));
            }

            throw new WebFaultException<string>(exeptionMessage == null ? message : exeptionMessage, HttpStatusCode.Unauthorized);
         }

    }
}