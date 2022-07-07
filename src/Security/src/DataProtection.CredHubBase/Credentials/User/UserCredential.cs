// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Credential for a user
/// </summary>
public class UserCredential : ICredentialValue
{
    /// <summary>
    /// Gets or sets name of the user
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Gets or sets password of the user
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets or sets hashed value of the password
    /// </summary>
    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; }
}
