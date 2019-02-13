using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.Security
{
    public interface ISecurityContext
    {
        bool HasClaim(EndpointClaim claim);
    }
}
