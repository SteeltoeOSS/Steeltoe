// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class SshGenerationRequest : CredHubGenerateRequest
    {
        private SshGenerationParameters _defaultParams = new SshGenerationParameters { KeyLength = CertificateKeyLength.Length_2048, SshComment = null };

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
            Parameters = parameters ?? _defaultParams;
            Mode = overwriteMode;
        }
    }
}
