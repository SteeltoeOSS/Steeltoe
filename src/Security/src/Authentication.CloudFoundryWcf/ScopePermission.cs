// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Claims;
using System.Security.Permissions;
using System.Web;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    [Serializable]
    public sealed class ScopePermission : IPermission, IUnrestrictedPermission
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
            if (!(HttpContext.Current.User is ClaimsPrincipal principal) || !principal.HasClaim("scope", Scope))
            {
                Console.Out.WriteLine("Access denied token is not in Scope: " + Scope);
                CloudFoundryWcfTokenValidator.ThrowJwtException("Access denied token does not have Scope: " + Scope, "insufficient_scope");
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
