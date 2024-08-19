// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Configuration;

/// <summary>
/// Indicates which actuators are exposed.
/// </summary>
public sealed class Exposure
{
    /// <summary>
    /// Gets the IDs of the actuators to include. Excluded entries take precedence.
    /// </summary>
    public IList<string> Include { get; } = new List<string>();

    /// <summary>
    /// Gets the IDs of the actuators to exclude. Takes precedence over included entries.
    /// </summary>
    public IList<string> Exclude { get; } = new List<string>();
}
