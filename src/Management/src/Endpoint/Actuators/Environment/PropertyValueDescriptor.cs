// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

public sealed class PropertyValueDescriptor(object? value, string? origin)
{
    [JsonPropertyName("value")]
    public object? Value { get; } = value;

    [JsonPropertyName("origin")]
    public string? Origin { get; } = origin;
}
