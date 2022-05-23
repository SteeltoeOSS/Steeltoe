// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public class ContextMappings
    {
        public ContextMappings()
        {
            var mappingList = new Dictionary<string, IList<MappingDescription>>()
            {
                { "dispatcherServlet", new List<MappingDescription>() }
            };
            Mappings = new Dictionary<string, IDictionary<string, IList<MappingDescription>>>()
            {
                { "dispatcherServlets", mappingList }
            };

            ParentId = null;
        }

        public ContextMappings(IDictionary<string, IList<MappingDescription>> mappingDict, string parentId = null)
        {
            // At this point, .NET will only ever has one context and it must be named "dispatcherServlets"
            // For .NET, the mappingDict contains keys that represent the type name of the controller and then a
            // list of MappingDescriptions for that controller.
            mappingDict ??= new Dictionary<string, IList<MappingDescription>>();

            if (mappingDict.Count == 0)
            {
                mappingDict.Add("dispatcherServlet", new List<MappingDescription>());
            }

            Mappings = new Dictionary<string, IDictionary<string, IList<MappingDescription>>>()
            {
                { "dispatcherServlets", mappingDict }
            };

            ParentId = parentId;
        }

        [JsonPropertyName("parentId")]
        public string ParentId { get; }

        [JsonPropertyName("mappings")]
        public IDictionary<string, IDictionary<string, IList<MappingDescription>>> Mappings { get; } // "dispatcherServlets", "dispatcherServlet"
    }
}
