// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Configuration;

/// <summary>
/// Represents the level of permissions required to access an endpoint, when running on Cloud Foundry.
/// </summary>
public enum EndpointPermissions
{
    /// <summary>
    /// Indicates no permission constraints.
    /// </summary>
    None,

    /// <summary>
    /// Indicates permission to access non-sensitive data.
    /// </summary>
    Restricted,

    /// <summary>
    /// Indicates permission to access sensitive data.
    /// </summary>
    Full
}
