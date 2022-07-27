// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CertificateCredential : ICredentialValue
{
    /// <summary>
    /// Gets or sets certificate of the Certificate Authority
    /// </summary>
    [JsonPropertyName("ca")]
    public string CertificateAuthority { get; set; }

    /// <summary>
    /// Gets or sets name of CA credential in credhub that has signed this certificate
    /// </summary>
    [JsonPropertyName("ca_name")]
    public string CertificateAuthorityName { get; set; }

    /// <summary>
    /// Gets or sets string representation of the certificate
    /// </summary>
    public string Certificate { get; set; }

    /// <summary>
    /// Gets or sets private key for the certificate
    /// </summary>
    [JsonPropertyName("private_key")]
    public string PrivateKey { get; set; }
}