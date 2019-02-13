using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.EndpointCore
{
    internal class CoreSecurityContext : ISecurityContext
    {
        private readonly HttpContext _context;

        public CoreSecurityContext(HttpContext context)
        {
            _context = context;
        }

        public bool HasClaim(EndpointClaim claim)
        {
            return _context != null
                && _context.User != null
                && claim != null && _context.User.HasClaim(claim.Type, claim.Value);
        }
    }
}
