// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Health.Contributors;

public sealed class DiskSpaceContributorOptions
{
    /// <summary>
    /// Gets or sets the disk space, in bytes, that is considered low. Default value: 10 MB.
    /// </summary>
    public long Threshold { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the path to check for available disk space. Default value: ".".
    /// </summary>
    public string? Path { get; set; } = ".";

    /// <summary>
    /// Gets or sets a value indicating whether to enable the disk space contributor. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
