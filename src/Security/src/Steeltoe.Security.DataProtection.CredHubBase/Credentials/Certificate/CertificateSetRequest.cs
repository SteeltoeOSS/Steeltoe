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
using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub
{
    public class CertificateSetRequest : CredentialSetRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateSetRequest"/> class.
        /// For writing a certificate to CredHub
        /// </summary>
        /// <param name="credentialName">Name of credential to set</param>
        /// <param name="privateKey">Private key value of credential to set</param>
        /// <param name="certificate">Certificate value of credential to set</param>
        /// <param name="certificateAuthority">Certificate authority value of credential to set</param>
        /// <param name="certificateAuthorityName">Name of CA credential in credhub that has signed this certificate</param>
        /// <param name="additionalPermissions">List of additional permissions to set on credential</param>
        /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
        /// <remarks>Must include either the CA or CA Name</remarks>
        public CertificateSetRequest(string credentialName, string privateKey, string certificate, string certificateAuthority = null, string certificateAuthorityName = null, List<CredentialPermission> additionalPermissions = null, OverwiteMode overwriteMode = OverwiteMode.noOverwrite)
        {
            if (!string.IsNullOrEmpty(certificateAuthority) && !string.IsNullOrEmpty(certificateAuthorityName))
            {
                throw new ArgumentException("You must specify either the CA Certificate or the name, not both");
            }

            Name = credentialName;
            Type = CredentialType.Certificate;
            Value = new CertificateCredential
            {
                PrivateKey = privateKey,
                Certificate = certificate,
                CertificateAuthority = certificateAuthority,
                CertificateAuthorityName = certificateAuthorityName
            };
            AdditionalPermissions = additionalPermissions;
            Mode = overwriteMode;
        }
    }
}
