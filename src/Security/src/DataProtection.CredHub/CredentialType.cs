// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

[JsonConverter(typeof(JsonStringEnumConverter))]
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
