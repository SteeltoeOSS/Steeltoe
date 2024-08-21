// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Hypermedia;

/// <summary>
/// A typed collection of links.
/// </summary>
public sealed class Links
{
    /// <summary>
    /// Gets or sets the type of links contained in this collection.
    /// </summary>
    public string Type { get; set; } = "steeltoe";

    /// <summary>
    /// Gets the list of links contained in this collection.
    /// </summary>
    [JsonPropertyName("_links")]
    public IDictionary<string, Link> Entries { get; } = new Dictionary<string, Link>();
}
