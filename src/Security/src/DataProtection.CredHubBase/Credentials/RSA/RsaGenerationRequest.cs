// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

public class RsaGenerationRequest : CredHubGenerateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RsaGenerationRequest"/> class.
    /// Use to request a new RSA Credential
    /// </summary>
    /// <param name="credentialName">Name of credential</param>
    /// <param name="keyLength">Optional Key Length (default: 2048)</param>
    /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite)</param>
    public RsaGenerationRequest(string credentialName, CertificateKeyLength keyLength = CertificateKeyLength.Length_2048, OverwiteMode overwriteMode = OverwiteMode.converge)
    {
        Name = credentialName;
        Type = CredentialType.RSA;
        Parameters = new KeyParameters { KeyLength = keyLength };
        Mode = overwriteMode;
    }
}