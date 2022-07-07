// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Steeltoe.Management.Endpoint.Security;
using System.Linq;

namespace Steeltoe.Management.Endpoint
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

        public string[] GetRequestComponents()
        {
            return _context.Request.Path.Value.Split('/');
        }

        public MediaTypeVersion? GetMediaType()
        {
            var requestHeaders = new RequestHeaders(_context.Request.Headers);
            return requestHeaders.Accept switch
            {
                { } accept when accept.Any(v => v.MediaType.Value == ActuatorMediaTypes.V3_JSON) => MediaTypeVersion.V3,
                { } accept when accept.Any(v => v.MediaType.Value == ActuatorMediaTypes.V2_JSON) => MediaTypeVersion.V2,
                { } accept when accept.Any(v => v.MediaType.Value == ActuatorMediaTypes.V1_JSON) => MediaTypeVersion.V1,
                _ => null
            };
        }
    }
}
