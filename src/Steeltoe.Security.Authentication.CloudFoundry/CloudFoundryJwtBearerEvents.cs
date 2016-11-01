using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class CloudFoundryJwtBearerEvents : JwtBearerEvents
    {
        public override Task TokenValidated(TokenValidatedContext context)
        {
            if (context == null)
            {
                return Task.FromResult(0);
            }

            var identity = context.Ticket.Principal.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return Task.FromResult(0);
            }

            var identifier = GetId(identity);
            if (!string.IsNullOrEmpty(identifier))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, identifier, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var givenName = GetGivenName(identity);
            if (!string.IsNullOrEmpty(givenName))
            {
                identity.AddClaim(new Claim(ClaimTypes.GivenName, givenName, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var familyName = GetFamilyName(identity);
            if (!string.IsNullOrEmpty(familyName))
            {
                identity.AddClaim(new Claim(ClaimTypes.Surname, familyName, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var email = GetEmail(identity);
            if (!string.IsNullOrEmpty(email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, email, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            var name = GetName(identity);
            if (!string.IsNullOrEmpty(name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name, name, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            return Task.FromResult(0);
        }

        private string GetGivenName(IIdentity identity)
        {
            return GetClaim(identity, "given_name");
        }

        private string GetFamilyName(IIdentity identity)
        {
            return GetClaim(identity, "family_name");
        }

        private string GetEmail(IIdentity identity)
        {
            return GetClaim(identity, "email");
        }
        private string GetName(IIdentity identity)
        {
            return GetClaim(identity, "user_name");
        }
        private string GetId(IIdentity identity)
        {
            return GetClaim(identity, "user_id");
        }

        private string GetClaim(IIdentity identity, string claim)
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
