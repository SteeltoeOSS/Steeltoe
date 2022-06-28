// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter

namespace Steeltoe.Security.DataProtection.CredHub;

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
