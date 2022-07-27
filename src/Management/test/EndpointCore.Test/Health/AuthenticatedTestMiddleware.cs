// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health.Test;

internal class AuthenticatedTestMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticatedTestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var claimsIdentity = new ClaimsIdentity(new List<Claim> { new Claim("healthdetails", "show") }, "TestAuthentication");
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        context.User = claimsPrincipal;

        await _next(context);
    }
}