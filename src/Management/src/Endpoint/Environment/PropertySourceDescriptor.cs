// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Environment;

public sealed class PropertySourceDescriptor
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("properties")]
    public IDictionary<string, PropertyValueDescriptor> Properties { get; }

    public PropertySourceDescriptor(string name, IDictionary<string, PropertyValueDescriptor> properties)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNull(properties);

        Name = name;
        Properties = properties;
    }
}
