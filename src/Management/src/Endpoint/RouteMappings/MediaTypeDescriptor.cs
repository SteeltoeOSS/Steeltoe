// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class MediaTypeDescriptor
{
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; }

    [JsonPropertyName("negated")]
    public bool Negated { get; set; }
}
