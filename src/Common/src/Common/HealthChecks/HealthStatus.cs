// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.CasingConventions;

namespace Steeltoe.Common.HealthChecks;

/// <summary>
/// Represents the reported status of a health check.
/// </summary>
[JsonConverter(typeof(SnakeCaseAllCapsEnumMemberJsonConverter))]
public enum HealthStatus
{
    /// <summary>
    /// Indicates that the component is in an unknown state.
    /// </summary>
    Unknown,

    /// <summary>
    /// Indicates that the component is healthy.
    /// </summary>
    Up,

    /// <summary>
    /// Indicates that the component is in a warning state.
    /// </summary>
    Warning,

    /// <summary>
    /// Indicates that the component is under maintenance (planned downtime).
    /// </summary>
    OutOfService,

    /// <summary>
    /// Indicates that the component is unhealthy, or an unhandled exception was thrown while executing the health check.
    /// </summary>
    Down
}
