using Steeltoe.Management.Endpoint.Security;
using System;
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
