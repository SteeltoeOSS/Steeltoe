// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.Endpoint;

internal sealed class CoreSecurityContext : ISecurityContext
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
}
