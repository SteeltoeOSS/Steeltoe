// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Env
{
    public class PropertySourceDescriptor
    {
        public PropertySourceDescriptor(string name, IDictionary<string, PropertyValueDescriptor> properties)
        {
            Name = name;
            Properties = properties;
        }

        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("properties")]
        public IDictionary<string, PropertyValueDescriptor> Properties;
    }
}
