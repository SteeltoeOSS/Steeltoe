// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Security.Test
{
    /// <summary>
    /// Adds a fake claim for Testing Authenticate test cases.
    /// </summary>
    internal class SetsUserInContextForTestsMiddleware
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
            if (context.Request.Headers.Keys.Contains(TestingHeader))
            {
                var claimsIdentity = new ClaimsIdentity(TestFakeAuthentication);
                claimsIdentity.AddClaim(new Claim("scope", "actuator.read"));
                context.User = new ClaimsPrincipal(claimsIdentity);
            }

            await _next(context);
        }
    }
}
