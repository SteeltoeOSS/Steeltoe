// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry;

public sealed class CloudFoundryService
{
    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a label describing the type of service.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets a list of tags describing the service.
    /// </summary>
    public IList<string> Tags { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the plan level at which the service is provisioned.
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// Gets the subtree of credentials. Each entry is either a single string value or a dictionary.
    /// </summary>
    public CloudFoundryCredentials Credentials { get; } = new();
}
