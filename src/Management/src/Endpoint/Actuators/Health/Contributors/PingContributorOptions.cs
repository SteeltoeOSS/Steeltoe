// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

internal sealed class PingContributorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable the ping contributor. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
