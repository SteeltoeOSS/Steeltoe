// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ScopePermissionAttribute : CodeAccessSecurityAttribute
    {
        public string Scope { get; set; }

        public ScopePermissionAttribute(SecurityAction action)
            : base(action)
        {
        }

        public override IPermission CreatePermission()
        {
            return new ScopePermission(Thread.CurrentPrincipal.Identity.Name, Scope);
        }
    }
}