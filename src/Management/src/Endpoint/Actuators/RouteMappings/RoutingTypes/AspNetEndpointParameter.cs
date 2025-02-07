// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

/// <summary>
/// Provides unified information about a parameter in an ASP.NET Core endpoint.
/// </summary>
internal sealed class AspNetEndpointParameter
{
    private static readonly AspNetEndpointParameterOrigin[] OriginsInOrderOfPriority =
    [
        AspNetEndpointParameterOrigin.Route,
        AspNetEndpointParameterOrigin.QueryString,
        AspNetEndpointParameterOrigin.Header,
        AspNetEndpointParameterOrigin.Other,
        AspNetEndpointParameterOrigin.Unknown
    ];

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

    public AspNetEndpointParameter MergeWith(AspNetEndpointParameter other)
    {
        AspNetEndpointParameterOrigin origin = MergeOriginWith(other.Origin);
        return new AspNetEndpointParameter(origin, other.Name, other.DefaultValue, other.IsRequired);
    }

    private AspNetEndpointParameterOrigin MergeOriginWith(AspNetEndpointParameterOrigin other)
    {
        List<AspNetEndpointParameterOrigin> originsToSort =
        [
            other,
            Origin
        ];

        originsToSort.Sort(OriginsComparison);
        return originsToSort[0];
    }

    private static int OriginsComparison(AspNetEndpointParameterOrigin left, AspNetEndpointParameterOrigin right)
    {
        int leftIndex = Array.IndexOf(OriginsInOrderOfPriority, left);
        int rightIndex = Array.IndexOf(OriginsInOrderOfPriority, right);
        return leftIndex.CompareTo(rightIndex);
    }

    public override string ToString()
    {
        string marker = IsRequired switch
        {
            true => "!",
            false => "?",
            _ => string.Empty
        };

        return $"{Origin}: {Name}{marker}{(DefaultValue != null ? "=" + DefaultValue : string.Empty)}";
    }
}
