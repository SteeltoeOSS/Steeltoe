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
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.EndpointCore
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
    }
}
