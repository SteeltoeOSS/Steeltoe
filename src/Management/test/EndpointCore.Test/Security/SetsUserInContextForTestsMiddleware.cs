// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Security.Test;

/// <summary>
/// Adds a fake claim for Testing Authenticate test cases.
/// </summary>
internal sealed class SetsUserInContextForTestsMiddleware
{
    public const string TestFakeAuthentication = "TestFakeAuthentication";
    public const string TestingHeader = "X-Test-Header";

    private readonly RequestDelegate _next;

    public SetsUserInContextForTestsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(TestingHeader))
        {
            var claimsIdentity = new ClaimsIdentity(TestFakeAuthentication);
            claimsIdentity.AddClaim(new Claim("scope", "actuator.read"));
            context.User = new ClaimsPrincipal(claimsIdentity);
        }

        await _next(context);
    }
}
