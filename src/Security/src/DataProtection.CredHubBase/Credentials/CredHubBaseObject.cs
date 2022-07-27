// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Common properties for CredHub requests
/// </summary>
public partial class CredHubBaseObject
{
    /// <summary>
    /// Gets or sets name of Credential
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets type of Credential
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CredentialType Type { get; set; }
}