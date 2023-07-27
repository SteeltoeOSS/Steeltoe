// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.RouteMappings;

public sealed class RouteMappingDescription
{
    internal const string AllHttpMethods = "GET || PUT || POST || DELETE || HEAD || OPTIONS";

    [JsonPropertyName("handler")]
    public string Handler { get; }

    [JsonPropertyName("predicate")]
    public string Predicate { get; }

    [JsonPropertyName("details")]
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public object Details { get; } // Always null for .NET

    public RouteMappingDescription(string routeHandler, AspNetCoreRouteDetails routeDetails)
    {
        ArgumentGuard.NotNull(routeHandler);
        ArgumentGuard.NotNull(routeDetails);

        Predicate = CreatePredicateString(routeDetails);
        Handler = routeHandler;
    }

    public RouteMappingDescription(MethodInfo routeHandler, AspNetCoreRouteDetails routeDetails)
    {
        ArgumentGuard.NotNull(routeHandler);
        ArgumentGuard.NotNull(routeDetails);

        Predicate = CreatePredicateString(routeDetails);
        Handler = CreateHandlerString(routeHandler);
    }

    private string CreateHandlerString(MethodInfo actionHandlerMethod)
    {
        return actionHandlerMethod.ToString();
    }

    private string CreatePredicateString(AspNetCoreRouteDetails routeDetails)
    {
        var builder = new StringBuilder("{");

        builder.Append($"[{routeDetails.RouteTemplate}]");

        builder.Append(",methods=");
        builder.Append($"[{CreateRouteMethods(routeDetails.HttpMethods)}]");

        if (!IsNullOrEmpty(routeDetails.Produces))
        {
            builder.Append(",produces=");
            builder.Append($"[{string.Join(" || ", routeDetails.Produces)}]");
        }

        if (!IsNullOrEmpty(routeDetails.Consumes))
        {
            builder.Append(",consumes=");
            builder.Append($"[{string.Join(" || ", routeDetails.Consumes)}]");
        }

        builder.Append('}');
        return builder.ToString();
    }

    private string CreateRouteMethods(IList<string> httpMethods)
    {
        if (IsNullOrEmpty(httpMethods))
        {
            return AllHttpMethods;
        }

        return string.Join(" || ", httpMethods);
    }

    private static bool IsNullOrEmpty(ICollection<string> list)
    {
        if (list == null || list.Count == 0)
        {
            return true;
        }

        return false;
    }
}
