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
//

using System.Security;
using System.Security.Permissions;
using System;
using System.Threading;


namespace Steeltoe.Security.Authentication.CloudFoundry.Wcf
{
   
    [Serializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ScopePermissionAttribute : CodeAccessSecurityAttribute
    {

        public string ConfigurationName { get; set; }

        public string Scope { get; set; }

        public ScopePermissionAttribute(SecurityAction action)
            : base(action)
        {
        }

       
        public override IPermission CreatePermission()
        {
            string scope = null;
            if (!string.IsNullOrEmpty(ConfigurationName))
            {
                scope = Environment.GetEnvironmentVariable(ConfigurationName);
            }

            scope = scope ?? Scope;

            return new ScopePermission(Thread.CurrentPrincipal.Identity.Name, scope);   
        }
    }
}