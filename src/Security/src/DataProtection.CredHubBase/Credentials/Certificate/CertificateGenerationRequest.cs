// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

public class CertificateGenerationRequest : CredHubGenerateRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateGenerationRequest"/> class.
    /// Use to request a new Certificate.
    /// </summary>
    /// <param name="credentialName">Name of the credential.</param>
    /// <param name="parameters">Variables for certificate generation.</param>
    /// <param name="overwriteMode">Overwrite existing credential (default: no-overwrite).</param>
    public CertificateGenerationRequest(string credentialName, CertificateGenerationParameters parameters, OverwriteMode overwriteMode = OverwriteMode.Converge)
    {
        var subjects = new List<string> { parameters.CommonName, parameters.Organization, parameters.OrganizationUnit, parameters.Locality, parameters.State, parameters.Country };
        if (!AtLeastOneProvided(subjects))
        {
            throw new ArgumentException("At least one subject value, such as common name or organization must be defined to generate the certificate");
        }

        if (string.IsNullOrEmpty(parameters.CertificateAuthority) && !parameters.IsCertificateAuthority && !parameters.SelfSign)
        {
            throw new ArgumentException("At least one signing parameter must be specified");
        }

        Name = credentialName;
        Type = CredentialType.Certificate;
        Parameters = parameters;
        Mode = overwriteMode;
    }

    private bool AtLeastOneProvided(List<string> parameters)
    {
        foreach (var s in parameters)
        {
            if (!string.IsNullOrEmpty(s))
            {
                return true;
            }
        }

        return false;
    }
}
