// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class PrincipalPermissionEnvAttribute : CodeAccessSecurityAttribute
    {
        private readonly bool _authenticated;

        public string ConfigurationName { get; set; }

        public string Role { get; set; }

        public PrincipalPermissionEnvAttribute(SecurityAction action)
            : base(action)
        {
            _authenticated = true;
        }

        public override IPermission CreatePermission()
        {
            if (Unrestricted)
            {
                return new PrincipalPermission(PermissionState.Unrestricted);
            }

            var matchACL = Environment.GetEnvironmentVariable(Role);
            if (string.IsNullOrEmpty(matchACL))
            {
                CloudFoundryWcfTokenValidator.ThrowJwtException("Configuration for not provided for Role: " + Role, "insufficient_scope");
            }

            var principal = Thread.CurrentPrincipal;

            if (principal.IsInRole(matchACL))
            {
                return new PrincipalPermission(principal.Identity.Name, matchACL, _authenticated);
            }
            else
            {
                Console.Out.WriteLine("Access denied user is not in Role: " + Role);
                CloudFoundryWcfTokenValidator.ThrowJwtException("Access denied user is not in Role: " + Role, "insufficient_scope");
                return null;
           }
        }
    }
}