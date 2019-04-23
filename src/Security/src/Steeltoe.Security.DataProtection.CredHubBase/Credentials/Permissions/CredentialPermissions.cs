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

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    /// <summary>
    /// Internal use: for request/response with permissions endpoints
    /// </summary>
    internal class CredentialPermissions
    {
        /// <summary>
        /// Gets or sets name of the credential with permissions
        /// </summary>
        [JsonProperty("credential_name")]
        public string CredentialName { get; set; }

        /// <summary>
        /// Gets or sets list of actors and their permissions for access to this credential
        /// </summary>
        public List<CredentialPermission> Permissions { get; set; }
    }
}
