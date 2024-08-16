// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class RouteMappingsResponse
{
    [JsonPropertyName("contexts")]
    public IDictionary<string, ContextMappings> ContextMappings { get; }

    public RouteMappingsResponse(ContextMappings contextMappings)
    {
        ArgumentNullException.ThrowIfNull(contextMappings);

        // At this point, .NET will only ever have one application => "application"
        ContextMappings = new Dictionary<string, ContextMappings>
        {
            { "application", contextMappings }
        };
    }
}
