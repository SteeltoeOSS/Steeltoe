// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

internal sealed class AuthenticatedTestMiddleware(RequestDelegate? next)
{
    private readonly RequestDelegate? _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var claimsIdentity = new ClaimsIdentity(new List<Claim>
        {
            new("health-details", "show")
        }, "TestAuthentication");

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        context.User = claimsPrincipal;

        if (_next != null)
        {
            await _next(context);
        }
    }
}
