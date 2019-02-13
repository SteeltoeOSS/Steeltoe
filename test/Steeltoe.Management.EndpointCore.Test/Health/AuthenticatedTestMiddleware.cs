using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health.Test
{
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
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            context.User = claimsPrincipal;

            await _next(context);
        }
    }
}
