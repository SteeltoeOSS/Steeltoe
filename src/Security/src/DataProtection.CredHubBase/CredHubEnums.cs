// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

public enum CredentialType
{
    Value,
    Password,
    User,
    JSON,
    Certificate,
    RSA,
    SSH
}

public enum CertificateKeyLength
{
    Length_2048 = 2048,
    Length_3072 = 3072,
    Length_4096 = 4096
}

#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter
/// <summary>
/// Overwrite mode for existing credentials (https://credhub-api.cfapps.io/#overwriting-credential-values).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OverwiteMode
{
    [EnumMember(Value = "no-overwrite")]
    noOverwrite,
    overwrite,
    converge
}

/// <summary>
/// Uses for certificates.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeyUsage
{
    digital_signature,
    non_repudiation,
    key_encipherment,
    data_encipherment,
    key_agreement,
    key_cert_sign,
    crl_sign,
    encipher_only,
    decipher_only
}

/// <summary>
/// Extended key usage for certificates.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExtendedKeyUsage
{
    [Description("Client Auth")]
    client_auth,
    server_auth,
    code_signing,
    email_protection,
    timestamping
}

/// <summary>
/// Operations that can be allowed for an actor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OperationPermissions
{
    read,
    write,
    delete,
    read_acl,
    write_acl
}
#pragma warning restore SA1300 // ElementMustBeginWithUpperCaseLetter
