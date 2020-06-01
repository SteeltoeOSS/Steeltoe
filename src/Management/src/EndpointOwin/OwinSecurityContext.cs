// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using Steeltoe.Management.Endpoint.Security;

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
