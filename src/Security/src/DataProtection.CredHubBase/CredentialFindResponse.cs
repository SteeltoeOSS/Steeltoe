// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Security.DataProtection.CredHub;

/// <summary>
/// Used internally to process results of a Find request.
/// </summary>
internal sealed class CredentialFindResponse
{
    /// <summary>
    /// Gets or sets credentials found by query.
    /// </summary>
    public List<FoundCredential> Credentials { get; set; }
}
