// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Json.Serialization;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings.RoutingTypes;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings.ResponseTypes;

public sealed class RouteDescriptor
{
    [JsonPropertyName("handler")]
    public string Handler { get; }

    [JsonPropertyName("predicate")]
    public string Predicate { get; }

    [JsonPropertyName("details")]
    public RouteDetailsDescriptor Details { get; }

    public RouteDescriptor(string handler, string predicate, RouteDetailsDescriptor details)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(details);

        Handler = handler;
        Predicate = predicate;
        Details = details;
    }

    internal static RouteDescriptor FromAspNetEndpoint(AspNetEndpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        string predicate = FormatSpringPredicate(endpoint);
        RouteDetailsDescriptor details = RouteDetailsDescriptor.FromAspNetEndpoint(endpoint);

        return new RouteDescriptor(endpoint.DisplayName, predicate, details);
    }

    private static string FormatSpringPredicate(AspNetEndpoint endpoint)
    {
        var builder = new StringBuilder("{");

        builder.Append(endpoint.HttpMethods.Count == 1 ? endpoint.HttpMethods.Single() : $"[{string.Join(", ", endpoint.HttpMethods)}]");
        builder.Append($" [{endpoint.RoutePattern}]");

        if (endpoint.Headers.Count > 0)
        {
            builder.Append(", headers ");
            builder.Append($"[{string.Join(", ", endpoint.Headers.Select(FormatHeaderInSpringPredicate))}]");
        }

        if (endpoint.Produces.Count > 0)
        {
            builder.Append(", produces ");
            builder.Append($"[{string.Join(" || ", endpoint.Produces)}]");
        }

        if (endpoint.Consumes.Count > 0)
        {
            builder.Append(", consumes ");
            builder.Append($"[{string.Join(" || ", endpoint.Consumes)}]");
        }

        builder.Append('}');
        return builder.ToString();
    }

    private static string FormatHeaderInSpringPredicate(AspNetEndpointParameter parameter)
    {
        StringBuilder builder = new();
        builder.Append(parameter.Name);

        if (parameter.DefaultValue != null)
        {
            builder.Append('=');
            builder.Append(parameter.DefaultValue);
        }

        return builder.ToString();
    }
}
