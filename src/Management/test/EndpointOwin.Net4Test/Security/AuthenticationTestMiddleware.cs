// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Owin;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.Security.Test
{
    internal class AuthenticationTestMiddleware : OwinMiddleware
    {
        public const string TestingCookieAuthentication = "TestCookieAuthentication";
        public const string TestingHeader = "X-Test-Header";

        public AuthenticationTestMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Headers.Keys.Contains(TestingHeader))
            {
                var claimsIdentity = new ClaimsIdentity(
                    new List<Claim>
                    {
                            new Claim("scope", "actuator.read")
                    },
                    TestingCookieAuthentication);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                context.Authentication.User = claimsPrincipal;
            }

            await Next.Invoke(context);
        }
    }
}
