// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Steeltoe.Security.DataProtection.CredHub;

public class CredHubCredential<T> : CredHubBaseObject
{
    /// <summary>
    /// Gets or sets when this (version of this) credential was created.
    /// </summary>
    [JsonPropertyName("version_created_at")]
    public DateTime VersionCreatedAt { get; set; }

    /// <summary>
    /// Gets or sets credential ID (assigned by CredHub).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets value of the credential.
    /// </summary>
    public T Value { get; set; }
}
