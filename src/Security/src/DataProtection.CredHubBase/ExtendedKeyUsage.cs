// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Text.Json.Serialization;

#pragma warning disable SA1300 // ElementMustBeginWithUpperCaseLetter

namespace Steeltoe.Security.DataProtection.CredHub;

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
