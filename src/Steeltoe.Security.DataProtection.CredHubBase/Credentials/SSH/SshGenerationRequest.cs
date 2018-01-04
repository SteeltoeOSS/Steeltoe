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

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class SshGenerationRequest : CredHubGenerateRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SshGenerationRequest"/> class.
        /// Use to request a new SSH Credential
        /// </summary>
        /// <param name="credentialName">Name of credential</param>
        /// <param name="keyLength">Optional Key Length (default: 2048)</param>
        /// <param name="overwrite">Overwrite existing credential (default: false)</param>
        /// <param name="sshComment">SSH comment of generated credential value (optional)</param>
        public SshGenerationRequest(string credentialName, CertificateKeyLength keyLength = CertificateKeyLength.Length_2048, bool overwrite = false, string sshComment = null)
        {
            Name = credentialName;
            Type = CredentialType.SSH;
            Parameters = new SshGenerationParameters
            {
                KeyLength = keyLength,
                SshComment = sshComment
            };
            Overwrite = overwrite;
        }
    }
}
