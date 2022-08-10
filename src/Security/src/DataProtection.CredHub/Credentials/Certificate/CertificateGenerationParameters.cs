// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CertificateGenerationParameters : KeyParameters
{
    /// <summary>
    /// Gets or sets common name of generated credential value.
    /// </summary>
    [JsonPropertyName("common_name")]
    public string CommonName { get; set; }

    /// <summary>
    /// Gets or sets alternative names of generated credential value.
    /// </summary>
    [JsonPropertyName("alternative_names")]
    public List<string> AlternativeNames { get; set; }

    /// <summary>
    /// Gets or sets organization of generated credential value.
    /// </summary>
    public string Organization { get; set; }

    /// <summary>
    /// Gets or sets organization Unit of generated credential value.
    /// </summary>
    [JsonPropertyName("organization_unit")]
    public string OrganizationUnit { get; set; }

    /// <summary>
    /// Gets or sets locality/city of generated credential value.
    /// </summary>
    public string Locality { get; set; }

    /// <summary>
    /// Gets or sets state/province of generated credential value.
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Gets or sets country of generated credential value.
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// Gets or sets key usage values.
    /// </summary>
    [JsonPropertyName("key_usage")]
    public List<KeyUsage> KeyUsage { get; set; }

    /// <summary>
    /// Gets or sets extended key usage values.
    /// </summary>
    [JsonPropertyName("extended_key_usage")]
    public List<ExtendedKeyUsage> ExtendedKeyUsage { get; set; }

    /// <summary>
    /// Gets or sets duration in days of generated credential value.
    /// </summary>
    public int Duration { get; set; } = 365;

    /// <summary>
    /// Gets or sets name of certificate authority to sign of generated credential value.
    /// </summary>
    [JsonPropertyName("ca")]
    public string CertificateAuthority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether whether to generate credential value as a certificate authority.
    /// </summary>
    [JsonPropertyName("is_ca")]
    public bool IsCertificateAuthority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether whether to self-sign generated credential value.
    /// </summary>
    [JsonPropertyName("self_sign")]
    public bool SelfSign { get; set; }
}
