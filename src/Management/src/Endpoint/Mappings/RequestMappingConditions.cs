// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Mappings;

public class RequestMappingConditions
{
    [JsonPropertyName("consumes")]
    public MediaTypeDescriptor[] Consumes  { get; set; }

    [JsonPropertyName("produces")]
    public MediaTypeDescriptor[] Produces { get; set; }

    [JsonPropertyName("headers")]
    public string [] Headers { get; set; }

    [JsonPropertyName("methods")]

    public string[] Methods { get; set; }

    [JsonPropertyName("patterns")]
    public string[] Patterns { get; set; }
}
