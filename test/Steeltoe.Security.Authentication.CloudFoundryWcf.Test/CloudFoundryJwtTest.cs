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
