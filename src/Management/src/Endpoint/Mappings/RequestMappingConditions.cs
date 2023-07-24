// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Mappings;

public class RequestMappingConditions
{
    [JsonPropertyName("consumes")]
    public IList<MediaTypeDescriptor> Consumes  { get; set; }

    [JsonPropertyName("produces")]
    public IList<MediaTypeDescriptor> Produces { get; set; }

    [JsonPropertyName("headers")]
    public IList<string> Headers { get; set; }

    [JsonPropertyName("methods")]

    public IList<string> Methods { get; set; }

    [JsonPropertyName("patterns")]
    public IList<string> Patterns { get; set; }
}
