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
    public class UserGenerationRequest : CredHubGenerateRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserGenerationRequest"/> class.
        /// Use to request a new <see cref="UserCredential"/>
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="parameters">Variables for username and password generation</param>
        /// <param name="additionalPermissions">List of additional permissions to set on credential</param>
        /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
        public UserGenerationRequest(string credentialName, UserGenerationParameters parameters, List<CredentialPermission> additionalPermissions = null, OverwiteMode overwriteMode = OverwiteMode.noOverwrite)
        {
            Name = credentialName;
            Type = CredentialType.User;
            Parameters = parameters;
            AdditionalPermissions = additionalPermissions;
            Mode = overwriteMode;
        }
    }
}
