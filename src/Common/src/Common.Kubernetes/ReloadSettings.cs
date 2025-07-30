// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Kubernetes;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class ReloadSettings
{
    /// <summary>
    /// Gets or sets the reload method (polling or event)
    /// </summary>
    public ReloadMethods Mode { get; set; } = ReloadMethods.Polling;

    /// <summary>
    /// Gets or sets the number of seconds before reloading config data
    /// </summary>
    public int Period { get; set; } = 15;

    /// <summary>
    /// Gets or sets a value indicating whether config maps should be reloaded if changed
    /// </summary>
    public bool ConfigMaps { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether secrets should be reloaded if changed
    /// </summary>
    public bool Secrets { get; set; } = false;
}

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public enum ReloadMethods
{
    Event,
    Polling
}