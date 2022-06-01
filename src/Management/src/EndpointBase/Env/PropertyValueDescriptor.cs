// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Env;

public class PropertyValueDescriptor
{
    public PropertyValueDescriptor(object value, string origin = null)
    {
        Value = value;
        Origin = origin;
    }

    [JsonPropertyName("value")]
    public object Value { get; }

    [JsonPropertyName("origin")]
    public string Origin { get; }
}
