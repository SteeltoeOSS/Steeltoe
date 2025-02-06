// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

/// <summary>
/// Provides unified information about a parameter in an ASP.NET endpoint.
/// </summary>
internal sealed class AspNetEndpointParameter
{
    public AspNetEndpointParameterOrigin Origin { get; }
    public string Name { get; }
    public object? DefaultValue { get; }
    public bool? IsRequired { get; }

    public AspNetEndpointParameter(AspNetEndpointParameterOrigin origin, string name, object? defaultValue, bool? isRequired)
    {
        ArgumentNullException.ThrowIfNull(name);

        Origin = origin;
        Name = name;
        DefaultValue = defaultValue == DBNull.Value ? null : defaultValue;
        IsRequired = isRequired;
    }

    public override string ToString()
    {
        string marker = IsRequired switch
        {
            true => "!",
            false => "?",
            _ => string.Empty
        };

        return $"{Name}{marker}{(DefaultValue != null ? "=" + DefaultValue : string.Empty)}";
    }
}
