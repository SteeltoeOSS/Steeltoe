// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Security.Test
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
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                    new List<Claim>
                    {
                            new Claim("scope", "actuator.read")
                    },
                    TestingCookieAuthentication);
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                context.Authentication.User = claimsPrincipal;
            }

            await Next.Invoke(context);
        }
    }
}
