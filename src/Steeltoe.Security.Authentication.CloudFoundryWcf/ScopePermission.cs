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

using System;
using System.Security;
using System.Security.Claims;
using System.Security.Permissions;
using System.Threading;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    [Serializable]
    public sealed class ScopePermission : IPermission, ISecurityEncodable, IUnrestrictedPermission
    {
        public string Scope { get; set; }

        public ScopePermission(string name, string scope)
        {
            Scope = scope;
        }

        public bool IsUnrestricted()
        {
            return true;
        }

        public void Demand()
        {
            ClaimsPrincipal principal = Thread.CurrentPrincipal as ClaimsPrincipal;

            if (principal == null || !principal.HasClaim("scope", this.Scope))
            {
                Console.Out.WriteLine("Access denied token is not in Scope: " + Scope);
                CloudFoundryTokenValidator.ThrowJwtException("Access denied token does not have Scope: " + Scope, "insufficient_scope");
            }
        }

        public IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }

            return new ScopePermission(null, Scope);
        }

        public bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return false;
            }

            return true;
        }

        public IPermission Union(IPermission target)
        {
            if (target == null)
            {
                return null;
            }

            return new ScopePermission(null, Scope);
        }

        public void FromXml(SecurityElement e)
        {
            throw new NotImplementedException();
        }

        public SecurityElement ToXml()
        {
            throw new NotImplementedException();
        }

        public IPermission Copy()
        {
            throw new NotImplementedException();
        }
    }
}
