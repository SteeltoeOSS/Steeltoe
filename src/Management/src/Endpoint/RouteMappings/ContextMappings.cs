// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class ContextMappings
{
    [JsonPropertyName("parentId")]
    public string? ParentId { get; }

    [JsonPropertyName("mappings")]
    public IDictionary<string, IDictionary<string, IList<RouteMappingDescription>>> Mappings { get; } // "dispatcherServlets", "dispatcherServlet"

    public ContextMappings(IDictionary<string, IList<RouteMappingDescription>> mappingDictionary, string? parentId)
    {
        ArgumentNullException.ThrowIfNull(mappingDictionary);

        // At this point, .NET will only ever have one context, and it must be named "dispatcherServlets"
        // For .NET, the mappingDictionary contains keys that represent the type name of the controller and then a
        // list of MappingDescriptions for that controller.

        if (mappingDictionary.Count == 0)
        {
            mappingDictionary.Add("dispatcherServlet", new List<RouteMappingDescription>());
        }

        Mappings = new Dictionary<string, IDictionary<string, IList<RouteMappingDescription>>>
        {
            { "dispatcherServlets", mappingDictionary }
        };

        ParentId = parentId;
    }
}
