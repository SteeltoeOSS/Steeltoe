// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf.Test
{
    public class CloudFoundryJwtTest
    {
        [Fact]
        public void ClaimsReMapped_WhenPresent()
        {
            // arrange
            var claims = new List<Claim>
            {
                new Claim("client_id", "clientId"),
                new Claim("user_id", "nameId"),
                new Claim("given_name", "First_Name"),
                new Claim("family_name", "Last_Name"),
            };
            var jwt = new JwtSecurityToken(issuer: "uaa");
            var identity = new ClaimsIdentity(claims);

            // act
            CloudFoundryJwt.OnTokenValidatedAddClaims(identity, jwt);

            // assert
            Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "nameId");
            Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.GivenName && c.Value == "First_Name");
            Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Surname && c.Value == "Last_Name");
            Assert.DoesNotContain(identity.Claims, c => c.Type == ClaimTypes.Email);
            Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Name && c.Value == "clientId");
        }
    }
}
