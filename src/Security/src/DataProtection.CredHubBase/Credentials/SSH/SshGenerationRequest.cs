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
    public class SshGenerationRequest : CredHubGenerateRequest
    {
        private SshGenerationParameters defaultParams = new SshGenerationParameters { KeyLength = CertificateKeyLength.Length_2048, SshComment = null };

        /// <summary>
        /// Initializes a new instance of the <see cref="SshGenerationRequest"/> class.
        /// Use to request a new SSH Credential
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="parameters">Optional parameters (defaults to key length 2048 and no SSH Comment)</param>
        /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
        public SshGenerationRequest(string credentialName, SshGenerationParameters parameters = null, OverwiteMode overwriteMode = OverwiteMode.converge)
        {
            Name = credentialName;
            Type = CredentialType.SSH;
            Parameters = parameters ?? defaultParams;
            Mode = overwriteMode;
        }
    }
}
