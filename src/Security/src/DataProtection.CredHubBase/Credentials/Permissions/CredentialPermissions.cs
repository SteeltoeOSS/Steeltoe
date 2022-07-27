// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Internal use: for request/response with permissions endpoints
/// </summary>
internal class CredentialPermissions
{
    /// <summary>
    /// Gets or sets name of the credential with permissions
    /// </summary>
    [JsonPropertyName("credential_name")]
    public string CredentialName { get; set; }

    /// <summary>
    /// Gets or sets list of actors and their permissions for access to this credential
    /// </summary>
    public List<CredentialPermission> Permissions { get; set; }
}