// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Path to a credential in CredHub
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class CredentialPath
{
    /// <summary>
    /// Gets or sets path containing one or more credentials
    /// </summary>
    public string Path { get; set; }
}