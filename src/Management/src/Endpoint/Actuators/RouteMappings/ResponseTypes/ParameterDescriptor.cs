// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class ParameterDescriptor
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("value")]
    public object? Value { get; }

    // Doesn't exist in Spring, but helpful to include.
    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Required { get; }

    // Not possible in ASP.NET Core.
    [JsonPropertyName("negated")]
    public bool Negated { get; }

    public ParameterDescriptor(string name, object? value, bool? required)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Value = value;
        Required = required;
    }

    internal static ParameterDescriptor FromAspNetEndpointParameter(AspNetEndpointParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return new ParameterDescriptor(parameter.Name, parameter.DefaultValue, parameter.IsRequired);
    }
}
