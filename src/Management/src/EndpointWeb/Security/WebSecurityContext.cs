// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Security;
using System.Security.Claims;
using System.Web;

namespace Steeltoe.Management.EndpointWeb.Security
{
    internal class WebSecurityContext : ISecurityContext
    {
        private readonly HttpContextBase _context;

        public WebSecurityContext(HttpContextBase context)
        {
           _context = context;
        }

        public bool HasClaim(EndpointClaim claim)
        {
            var user = _context.User;
            return claim != null &&
                    user != null &&
                    user.Identity.IsAuthenticated && ((ClaimsIdentity)user.Identity).HasClaim(claim.Type, claim.Value);
        }
    }
}
