// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    public static class CloudFoundryJwt
    {
        public static void OnTokenValidatedAddClaims(ClaimsIdentity identity, JwtSecurityToken jwt)
        {
            AddClaimIfNotNullOrEmpty(identity, "user_id", ClaimTypes.NameIdentifier, jwt.Issuer);
            AddClaimIfNotNullOrEmpty(identity, "given_name", ClaimTypes.GivenName, jwt.Issuer);
            AddClaimIfNotNullOrEmpty(identity, "family_name", ClaimTypes.Surname, jwt.Issuer);
            AddClaimIfNotNullOrEmpty(identity, "email", ClaimTypes.Email, jwt.Issuer);

            if (!AddClaimIfNotNullOrEmpty(identity, "user_name", ClaimTypes.Name, jwt.Issuer))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, GetClientId(identity), ClaimValueTypes.String, jwt.Issuer));
            }
        }

        /// <summary>
        /// Add a claim to the identity from another location, if found
        /// </summary>
        /// <param name="identity">The identity to investicate and modify</param>
        /// <param name="claimLocator">The claim we're trying to get</param>
        /// <param name="claimType">The claim we're trying to set</param>
        /// <param name="jwtIssuer">Issuer of the JWT</param>
        /// <returns>True if the claim was not null or empty (and was added)</returns>
        private static bool AddClaimIfNotNullOrEmpty(ClaimsIdentity identity, string claimLocator, string claimType, string jwtIssuer)
        {
            var claimValue = GetClaim(identity, claimLocator);
            if (!string.IsNullOrEmpty(claimValue))
            {
                identity.AddClaim(new Claim(claimType, claimValue, ClaimValueTypes.String, jwtIssuer));
                return true;
            }

            return false;
        }

        private static string GetClientId(IIdentity identity) => GetClaim(identity, "client_id");

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
    }
}
