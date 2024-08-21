// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health;

/// <summary>
/// Represents the level of detail to include in the health endpoint response.
/// </summary>
public enum ShowDetails
{
    /// <summary>
    /// Always include health check details in the response.
    /// </summary>
    Always,

    /// <summary>
    /// Never include health check details in the response.
    /// </summary>
    Never,

    /// <summary>
    /// Only include health check details in the response when the user is authorized.
    /// </summary>
    WhenAuthorized
}
