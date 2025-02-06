// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class MediaTypeDescriptor
{
    [JsonPropertyName("mediaType")]
    public string MediaType { get; }

    // Not possible in ASP.NET.
    [JsonPropertyName("negated")]
    public bool Negated { get; }

    public MediaTypeDescriptor(string mediaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);

        MediaType = mediaType;
    }
}
