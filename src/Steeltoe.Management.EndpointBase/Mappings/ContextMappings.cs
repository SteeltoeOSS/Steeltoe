// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System.Collections.Generic;

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
            Mappings = new Dictionary<string, IDictionary<string, IList<MappingDescription>>>()
            {
                { "dispatcherServlets", mappingDict }
            };

            ParentId = parentId;
        }

        [JsonProperty("parentId")]
        public string ParentId { get; }

        [JsonProperty("mappings")]
        public IDictionary<string, IDictionary<string, IList<MappingDescription>>> Mappings { get; } // "dispatcherServlets", "dispatcherServlet"
    }
}
