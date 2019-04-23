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

using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class PasswordGenerationRequest : CredHubGenerateRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordGenerationRequest"/> class.
        /// Use to Request a new Password
        /// </summary>
        /// <param name="credentialName">Name of the credential</param>
        /// <param name="parameters">Variables for password generation</param>
        /// <param name="additionalPermissions">List of additional permissions to set on credential</param>
        /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
        public PasswordGenerationRequest(string credentialName, PasswordGenerationParameters parameters, List<CredentialPermission> additionalPermissions = null, OverwiteMode overwriteMode = OverwiteMode.noOverwrite)
        {
            Name = credentialName;
            Type = CredentialType.Password;
            Parameters = parameters;
            AdditionalPermissions = additionalPermissions;
            Mode = overwriteMode;
        }
    }
}
