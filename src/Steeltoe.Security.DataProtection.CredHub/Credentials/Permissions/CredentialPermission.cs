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

using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class CredentialPermission
    {
        /// <summary>
        /// Schemed string identifying an actor -- auth_type:scope/primary_identifier
        /// </summary>
        public string Actor { get; set; }

        /// <summary>
        /// List of operations permissioned for the actor
        /// </summary>
        public List<OperationPermissions> Operations { get; set; }
    }
}
