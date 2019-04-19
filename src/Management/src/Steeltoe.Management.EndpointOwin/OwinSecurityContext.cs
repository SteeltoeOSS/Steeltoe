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

using Microsoft.Owin;
using Steeltoe.Management.Endpoint.Security;

namespace Steeltoe.Management.EndpointOwin
{
    internal class OwinSecurityContext : ISecurityContext
    {
        private readonly IOwinContext _owinContext;

        public OwinSecurityContext(IOwinContext owinContext)
        {
            _owinContext = owinContext;
        }

        public bool HasClaim(EndpointClaim claim)
        {
           var user = _owinContext.Authentication.User;
           return user != null && claim != null && user.HasClaim(claim.Type, claim.Value);
        }
    }
}
