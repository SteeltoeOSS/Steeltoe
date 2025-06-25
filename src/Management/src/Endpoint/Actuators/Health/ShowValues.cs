// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health;

/// <summary>
/// Represents when something should be included in an endpoint response.
/// </summary>
public enum ShowValues
{
    /// <summary>
    /// Never include this item.
    /// </summary>
    Never,

    /// <summary>
    /// Only include this item when the user is authorized.
    /// </summary>
    WhenAuthorized,

    /// <summary>
    /// Always include this item.
    /// </summary>
    Always
}
