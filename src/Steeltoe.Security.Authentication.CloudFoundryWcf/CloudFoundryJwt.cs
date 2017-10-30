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

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;


namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public class CloudFoundryJwt
    {

        public static void OnTokenValidatedAddClaims(ClaimsIdentity identity,JwtSecurityToken jwt)
        {
           
            var identifier = GetId(identity);
            if (!string.IsNullOrEmpty(identifier))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier, ClaimValueTypes.String, jwt.Issuer));
            }

            var givenName = GetGivenName(identity);
            if (!string.IsNullOrEmpty(givenName))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, givenName, ClaimValueTypes.String, jwt.Issuer));
            }

            var familyName = GetFamilyName(identity);
            if (!string.IsNullOrEmpty(familyName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Surname, familyName, ClaimValueTypes.String, jwt.Issuer));
            }

            var email = GetEmail(identity);
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, email, ClaimValueTypes.String, jwt.Issuer));
            }

            var name = GetName(identity);
            if (!string.IsNullOrEmpty(name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, name, ClaimValueTypes.String, jwt.Issuer));
            }
            else
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, GetClientId(identity), ClaimValueTypes.String, jwt.Issuer));
            
            }
           
        }

      

        private static string GetGivenName(IIdentity identity)
        {
            return GetClaim(identity, "given_name");
        }

        private static string GetFamilyName(IIdentity identity)
        {
            return GetClaim(identity, "family_name");
        }

        private static string GetEmail(IIdentity identity)
        {
            return GetClaim(identity, "email");
        }
        private static string GetName(IIdentity identity)
        {
            return GetClaim(identity, "user_name");
        }
        private static string GetId(IIdentity identity)
        {
            return GetClaim(identity, "user_id");
        }

        private static string GetClientId(IIdentity identity)
        {
            return GetClaim(identity, "client_id");
        }

      

        private static string GetClaim(IIdentity identity, string claim)
        {
            var claims = identity as ClaimsIdentity;
            if (claims == null)
            {
                return null;
            }
            var idClaim = claims.FindFirst(claim);
            if (idClaim == null)
            {
                return null;
            }
            return idClaim.Value;
        }

        private static Claim[] GetClaims(IIdentity identity, string claim)
        {
            var claims = identity as ClaimsIdentity;
            if (claims == null)
            {
                return null;
            }
            var idClaims = claims.FindAll(claim);
            if (idClaims == null)
            {
                return null;
            }
            return idClaims.ToArray<Claim>();
        }

        private string GetClaimWithFallback(IEnumerable<Claim> claims, params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                if (claims.Count(c => c.Type == claimType) > 0)
                {
                    return claims.SingleOrDefault(c => c.Type == claimType).Value;
                }
            }

            return null;
        }
    }
}
