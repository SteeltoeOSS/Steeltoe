using Microsoft.Owin;
using Steeltoe.Management.Endpoint.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin
{
    internal class OwinSecurityContext : ISecurityContext
    {
        private readonly IOwinContext _owinContext;

        public OwinSecurityContext(IOwinContext owinContext)
        {
            _owinContext = owinContext;
        }

        public bool HasClaim(EndpointClaim claim)
        {
           var user = _owinContext.Authentication.User;
           return user != null && claim != null && user.HasClaim(claim.Type, claim.Value);
        }
    }
}
