// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CertificateSetRequest : CredentialSetRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateSetRequest"/> class.
    /// For writing a certificate to CredHub.
    /// </summary>
    /// <param name="credentialName">Name of credential to set.</param>
    /// <param name="privateKey">Private key value of credential to set.</param>
    /// <param name="certificate">Certificate value of credential to set.</param>
    /// <param name="certificateAuthority">Certificate authority value of credential to set.</param>
    /// <param name="certificateAuthorityName">Name of CA credential in credhub that has signed this certificate.</param>
    /// <remarks>Must include either the CA or CA Name.</remarks>
    public CertificateSetRequest(string credentialName, string privateKey, string certificate, string certificateAuthority = null, string certificateAuthorityName = null)
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
    }
}
