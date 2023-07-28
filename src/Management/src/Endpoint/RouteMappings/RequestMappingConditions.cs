// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class RequestMappingConditions
{
    [JsonPropertyName("consumes")]
    public IList<MediaTypeDescriptor> Consumes { get; set; } = new List<MediaTypeDescriptor>();

    [JsonPropertyName("produces")]
    public IList<MediaTypeDescriptor> Produces { get; set; } = new List<MediaTypeDescriptor>();

    [JsonPropertyName("headers")]
    public IList<string> Headers { get; set; } = new List<string>();

    [JsonPropertyName("methods")]
    public IList<string> Methods { get; set; } = new List<string>();

    [JsonPropertyName("patterns")]
    public IList<string> Patterns { get; set; } = new List<string>();
}
