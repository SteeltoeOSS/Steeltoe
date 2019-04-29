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

            string matchACL = Environment.GetEnvironmentVariable(Role);
            if (string.IsNullOrEmpty(matchACL))
            {
                CloudFoundryWcfTokenValidator.ThrowJwtException("Configuration for not provided for Role: " + Role, "insufficient_scope");
            }

            IPrincipal principal = Thread.CurrentPrincipal;

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